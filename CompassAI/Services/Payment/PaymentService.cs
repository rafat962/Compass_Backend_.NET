using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;

namespace CompassAI.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;

        public PaymentService(IConfiguration configuration)
        {
            _configuration = configuration;
            // تعيين الـ Secret Key لـ Stripe بمجرد حقن الخدمة
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<Session> CreateCheckoutSessionAsync(string userId, string packageType)
        {
            // تحديد السعر بناءً على الباقة (دي أسعار تجريبية بالدولار مثلاً سنتات * 100)
            long priceInCents = packageType.ToLower() switch
            {
                "basic" => 500,     // $5.00
                "gold" => 1000,     // $10.00
                "premium" => 5000,  // $50.00
                _ => 0              // Free
            };

            if (priceInCents == 0)
                throw new Exception("Free package does not require payment.");

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" }, // الدفع بالبطاقات
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = priceInCents,
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"GeoGenAI-Hub - {packageType.ToUpper()} Subscription",
                                Description = $"Monthly quota for {packageType} package"
                            },
                        },
                        Quantity = 1,
                    },
                },
                Mode = "payment", // دفع لمرة واحدة (أو subscription لو هتعملها متجددة تلقائي)

                // روابط العودة للفرونت إند بعد الدفع أو الإلغاء
                SuccessUrl = _configuration["Stripe:SuccessUrl"],
                CancelUrl = _configuration["Stripe:CancelUrl"],
                // الـ Metadata دي السحر كله! دي اللي هتوصل للـ Webhook عشان نعرف مين اللي دفع
                Metadata = new Dictionary<string, string>
                {
                    { "UserId", userId },
                    { "PackageType", packageType }
                }
            };

            var service = new SessionService();
            return await service.CreateAsync(options);
        }
    }
}