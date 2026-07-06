using System.ComponentModel.DataAnnotations;

namespace CompassAI.Models.DTO
{
    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        [MaxLength(255)]
        public string Email { get; set; }
        [Required]
        [MinLength(4)]
        [MaxLength(255)]
        public string Password { get; set; }
    }
}
