using System.ComponentModel.DataAnnotations;

namespace CompassAI.Models.DTO
{
    public class NewPasswordDto
    {
        [Required]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters long.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
        [Required]
        public string token { get; set; }
    }
}
