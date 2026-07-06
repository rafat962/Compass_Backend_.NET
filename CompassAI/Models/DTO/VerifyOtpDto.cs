using System.ComponentModel.DataAnnotations;

namespace CompassAI.Models.DTO
{
    public class VerifyOtpDto
    {
        [Required] public string OTP { get; set; }
        [Required] public string token { get; set; }
    }
}
