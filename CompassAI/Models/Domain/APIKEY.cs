 using System.ComponentModel.DataAnnotations;

namespace CompassAI.Models.Domain
{
    public class ApiKey
    {
        public Guid Id { get; set; }

        [Required]
        public string Key { get; set; } = string.Empty; // The unique key string


        // Package info stored with the key
        public string PackageType { get; set; } = "Free"; // Free, Basic, Gold, Premium

        // Quota Management
        public int RequestsLimit { get; set; } // Total questions allowed (e.g., 1000)
        public int MapTalkLimit { get; set; } = 0; // Total questions allowed (e.g., 1000)
        public int SpecReviewerLimit { get; set; } = 0; // Total questions allowed (e.g., 1000)
        public int DocQueryLimit { get; set; } = 0; // Total questions allowed (e.g., 1000)
        public int ArcProMCP { get; set; } = 0; // Total questions allowed (e.g., 1000)
        public int QGISMCP { get; set; } = 0; // Total questions allowed (e.g., 1000)
        public int RequestsUsed { get; set; }  // Counter: increments with each request

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Key expiration date

        // Foreign Key to User
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        // Helper property to check if key is valid
        public bool IsValid => IsActive &&
                            (ExpiresAt == null || ExpiresAt > DateTime.UtcNow) &&
                            (RequestsUsed < RequestsLimit);
    }
}
