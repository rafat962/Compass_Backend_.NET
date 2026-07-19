using CompassAI.Models.DTO;
using CompassAI.Repositories.APIKEY;
using CompassAI.Repositories.Auth;
using CompassAI.Services.Payment;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;

namespace CompassAI.Controllers.Payment
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IApikeyRepository _apiKeyRepository;
        private readonly IAuthRepository _authRepository;
        private readonly IConfiguration _configuration;

        public PaymentController(
            IPaymentService paymentService,
            IApikeyRepository apiKeyRepository,
            IAuthRepository authRepository,
            IConfiguration configuration)
        {
            _paymentService = paymentService;
            _apiKeyRepository = apiKeyRepository;
            _authRepository = authRepository;
            _configuration = configuration;
        }

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CheckoutRequestDto request)
        {
            try
            {
                var session = await _paymentService.CreateCheckoutSessionAsync(request.UserId, request.PackageType);
                return Ok(new { url = session.Url });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var stripeSignature = Request.Headers["Stripe-Signature"];

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    stripeSignature,
                    _configuration["Stripe:WebhookSecret"]
                );

                if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;

                    if (session?.Metadata == null ||
                        !session.Metadata.TryGetValue("UserId", out var userId) ||
                        !session.Metadata.TryGetValue("PackageType", out var packageType) ||
                        !Guid.TryParse(userId, out var userGuid))
                    {
                        return BadRequest(new { message = "Checkout session metadata is invalid." });
                    }


                    var userKeys = await _apiKeyRepository.GetByUserIdAsync(userGuid);
                    var activeKey = userKeys.FirstOrDefault(k => k.IsActive);

                    if (activeKey != null)
                    {
                        int calculatedLimit = packageType.ToLower() switch
                        {
                            "basic" => 500,
                            "gold" => 1000,
                            "premium" => 50000,
                            _ => 50
                        };

                        activeKey.PackageType = packageType;
                        activeKey.RequestsLimit = calculatedLimit;
                        activeKey.RequestsUsed = 0;
                        activeKey.MapTalkLimit = 0;
                        activeKey.SpecReviewerLimit = 0;
                        activeKey.DocQueryLimit = 0;
                        activeKey.ExpiresAt = DateTime.UtcNow.AddMonths(1);

                        await _apiKeyRepository.UpdateAsync(activeKey);
                    }

                    var user = await _authRepository.GetUserByIdAsync(userGuid);
                    if (user != null)
                    {
                        user.CurrentPlan = packageType;
                        user.UpdatedAt = DateTime.UtcNow;
                        await _authRepository.UpdateUserAsync(user);
                    }
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest(new { message = "Webhook verification failed", error = ex.Message });
            }
        }
    }
}
