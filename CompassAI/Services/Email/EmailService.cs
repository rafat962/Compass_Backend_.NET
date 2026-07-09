using MailKit.Net.Smtp;
using MimeKit;
using RazorLight;
using CompassAI.Models.Domain;
using CompassAI.Services.Email;
using CompassAI.Models.DTO;

namespace CompassAI.Services
{
    public class EmailService : IEmailService
    {
        private readonly IRazorLightEngine _engine;

        public EmailService()
        {
            // Ensure the path is correct relative to the executing assembly
            var rootPath = Path.Combine(AppContext.BaseDirectory, "Templates");

            // Debugging line to see where it's looking (Check your Console output)
            if (!Directory.Exists(rootPath))
            {
                Console.WriteLine($"Critical Error: Email Templates folder not found at {rootPath}");
            }

            _engine = new RazorLightEngineBuilder()
                .UseFileSystemProject(rootPath)
                .UseMemoryCachingProvider()
                .SetOperatingAssembly(typeof(Program).Assembly)
                .Build();
        }

        private async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Rafat Kamel", "rafatkamel96@gmail.com"));
            email.To.Add(new MailboxAddress(toName, toEmail));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync("rafatkamel96@gmail.com", "wevv zvln bwun pzof");
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the whole app
                Console.WriteLine($"Email delivery failed: {ex.Message}");
            }
        }

        public async Task SendOtpEmailAsync(User user, string url)
        {
            var model = new { FirstName = user.Name, OTP = user.OTP, Url = url };

            // تم التأكد من إزالة أي "/" زائدة هنا ليعمل الـ Engine بشكل صحيح
            string htmlBody = await _engine.CompileRenderAsync("EmailTemplate.cshtml", model);

            await SendEmailAsync(user.Email, user.Name, "Activate your account", htmlBody);
        }

        public async Task SendPasswordResetEmailAsync(User user, string url)
        {
            var model = new { UserName = user.Name, ResetUrl = url };

            // استدعاء مباشر باسم الملف
            string htmlBody = await _engine.CompileRenderAsync("ResetPassword.cshtml", model);

            await SendEmailAsync(user.Email, user.Name, "Reset Your Password - GeoPlatform", htmlBody);
        }

        public async Task SendWelcomeEmail(User user, string loginUrl)
        {
            var model = new { UserName = user.Name, LoginUrl = loginUrl };

            // استدعاء مباشر باسم الملف
            string htmlBody = await _engine.CompileRenderAsync("Welcome.cshtml", model);

            await SendEmailAsync(user.Email, user.Name, "Welcome to GeoPlatform - Account Activated!", htmlBody);
        }

        public async Task SendEmailToOwner(EmailToOwnerDto emailDto)
        {
            // Build a simple HTML body for the owner
            string htmlBody = $@"
            <h3>New Message from CompassAI Contact Form</h3>
            <p><strong>From:</strong> {emailDto.Email}</p>
            <p><strong>Subject:</strong> {emailDto.Subject}</p>
            <hr/>
            <p><strong>Message:</strong></p>
            <p>{emailDto.Message}</p>";

            // We send it to your business/owner email
            await SendEmailAsync("rafatkamel96@gmail.com", "CompassAI Owner", $"Support: {emailDto.Subject}", htmlBody);
        }
    }
}