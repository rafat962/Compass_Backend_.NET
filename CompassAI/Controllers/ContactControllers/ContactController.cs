using CompassAI.Models.DTO;
using CompassAI.Services.Email;
using Microsoft.AspNetCore.Mvc;

namespace CompassAI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // Endpoint for users to contact the site owner
        [HttpPost("send-to-owner")]
        public async Task<IActionResult> ContactOwner([FromBody] EmailToOwnerDto contactRequest)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _emailService.SendEmailToOwner(contactRequest);

                return Ok(new
                {
                    status = "success",
                    message = "Your message has been sent to the support team."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "error",
                    message = "Failed to send email. Please try again later."
                });
            }
        }
    }
}