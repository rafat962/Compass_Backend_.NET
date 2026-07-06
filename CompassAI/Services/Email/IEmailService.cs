using CompassAI.Models.Domain;
using CompassAI.Models.DTO;

namespace CompassAI.Services.Email
{
    public interface IEmailService
    {
            public Task SendOtpEmailAsync(User user, string url);
            public Task SendPasswordResetEmailAsync(User user, string url);
            public Task SendWelcomeEmail(User user, string loginUrl);
            public Task SendEmailToOwner(EmailToOwnerDto email);
    }
}
