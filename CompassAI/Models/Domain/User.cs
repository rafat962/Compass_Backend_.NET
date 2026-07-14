using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompassAI.Models.Domain
{
    public class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(3)]
        public string PasswordHash { get; set; }

        [NotMapped] // dont save it in database
        public string ConfirmPassword { get; set; }

        public string Role { get; set; } = "user";

        public List<UserPermission> Permissions { get; set; } = new();

        public bool Active { get; set; } = false;
        public bool EmailActive { get; set; } = false;

        public string Photo { get; set; } = "none";

        public List<DateTime> LoginLogs { get; set; } = new();
        public List<DateTime> LogoutLogs { get; set; } = new();

        public string? OTP { get; set; }

        public string? ResetPasswordToken { get; set; }
        public DateTime? ResetPasswordExpires { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Added for API Key Management ---

        // One-to-Many relationship with API Keys
        public List<ApiKey> ApiKeys { get; set; } = new();

        // Current plan assigned to the user profile (Optional, but good for UI)
        public string CurrentPlan { get; set; } = "Free";
    }
}