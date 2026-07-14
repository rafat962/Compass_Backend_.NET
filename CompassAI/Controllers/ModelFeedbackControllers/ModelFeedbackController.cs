using System;
using System.Security.Claims;
using System.Threading.Tasks;
using CompassAI.Models;
using CompassAI.Models.DTOs;
using CompassAI.Models.Enums;
using CompassAI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompassAI.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ModelFeedbackController : ControllerBase
    {
        private readonly IModelFeedbackRepository _feedbackRepository;

        public ModelFeedbackController(IModelFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository;
        }

        [HttpPost("user/{userId:guid}")]
        public async Task<IActionResult> CreateFeedback([FromRoute] Guid userId, [FromBody] CreateFeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { status = "error", message = "Validation failed", errors = ModelState });

            // الـ userId جاي جاهز كـ Guid من الـ Route مباشرة بفضل [FromRoute]
            var feedback = new ModelFeedback
            {
                Content = dto.Content,
                ModelTarget = dto.ModelTarget,
                Type = dto.Type,
                Rating = dto.Rating,
                UserPromptContext = dto.UserPromptContext,
                ModelResponseContext = dto.ModelResponseContext,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _feedbackRepository.AddAsync(feedback);
            return CreatedAtAction(nameof(GetFeedbackById), new { id = result.Id }, new { status = "success", data = result });
        }
        [HttpGet]
        public async Task<IActionResult> GetAllFeedbacks([FromQuery] ModelType? modelTarget, [FromQuery] FeedbackType? type)
        {
            var feedbacks = await _feedbackRepository.GetAllAsync(modelTarget, type);
            return Ok(feedbacks);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeedbackById(int id)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(id);
            if (feedback == null)
                return NotFound($"Feedback with ID {id} not found.");

            return Ok(feedback);
        }

        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetFeedbacksByUser([FromRoute] Guid userId)
        {
            // الـ Guid بيحصل له Parsing تلقائياً من الـ Route
            var feedbacks = await _feedbackRepository.GetByUserIdAsync(userId);

            return Ok(new
            {
                status = "success",
                data = feedbacks
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            var deleted = await _feedbackRepository.DeleteAsync(id);
            if (!deleted)
                return NotFound($"Feedback with ID {id} not found.");

            return NoContent();
        }
    }
}