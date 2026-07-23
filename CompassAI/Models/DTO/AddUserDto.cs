using System.ComponentModel.DataAnnotations;

namespace CompassAI.Models.DTO
{
    public class AddUserDto
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Role { get; set; } = "user";

        public string CurrentPlan { get; set; } = "Free";
    }
}
