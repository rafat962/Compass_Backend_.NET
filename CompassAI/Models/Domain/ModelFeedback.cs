using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CompassAI.Models.Domain;
using CompassAI.Models.Enums;

namespace CompassAI.Models
{
    public class ModelFeedback
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        public ModelType ModelTarget { get; set; }

        [Required]
        public FeedbackType Type { get; set; } = FeedbackType.General; 

        [Range(1, 5)]
        public int? Rating { get; set; } 

        public string? UserPromptContext { get; set; }  
        public string? ModelResponseContext { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User? User { get; set; }
    }
}