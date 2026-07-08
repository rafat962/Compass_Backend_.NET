using Stripe.Checkout;

namespace CompassAI.Services.Payment
{
    public interface IPaymentService
    {

        Task<Session> CreateCheckoutSessionAsync(string userId, string packageType);
    }
} 
