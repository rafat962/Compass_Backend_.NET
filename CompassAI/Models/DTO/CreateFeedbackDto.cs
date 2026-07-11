using System.ComponentModel.DataAnnotations;
using CompassAI.Models.Enums;

namespace CompassAI.Models.DTOs
{
    public class CreateFeedbackDto
    {
        [Required(ErrorMessage = "Content is required")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model target is required")]
        public ModelType ModelTarget { get; set; }

        [Required(ErrorMessage = "Feedback type is required")]
        public FeedbackType Type { get; set; } = FeedbackType.General;

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int? Rating { get; set; }

        public string? UserPromptContext { get; set; }
        public string? ModelResponseContext { get; set; }
    }
}