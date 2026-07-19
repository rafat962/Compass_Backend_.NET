using CompassAI.Models;
using CompassAI.Models.DTOs;
using CompassAI.Models.Enums;
using CompassAI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

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
        [HttpGet("admin/feedback-analysis")]
        [Authorize] 
        public async Task<IActionResult> GetFeedbackAnalysis()
        {
            try
            {
                
                var allFeedback = await _feedbackRepository.GetAllAsync();
                var totalFeedback = allFeedback.Count();

                // 2. توزيع أنواع الفيدباك (FeedbackType Pie Chart)
                // نفترض الـ Enum: Bug = 0, FeatureRequest = 1, Positive = 2 (أو حسب ترتيبكِ)
                var feedbackTypeData = allFeedback
                    .GroupBy(f => f.Type)
                    .Select(group => new
                    {
                        Name = group.Key.ToString(), // يحول الـ Enum لاسم مقروء تلقائياً مثل "Bug" أو "Positive"
                        Value = group.Count()
                    })
                    .ToList();

                // 3. توزيع الفيدباك حسب الموديل المستهدف (ModelTarget Pie Chart)
                // نفترض الـ Enum: MapTalk = 0, DocQuery = 1, SpecReviewer = 2
                var modelTargetData = allFeedback
                    .GroupBy(f => f.ModelTarget)
                    .Select(group => new
                    {
                        Name = group.Key.ToString(), // اسم الخدمة/الموديل مثل "MapTalk"
                        Value = group.Count()
                    })
                    .ToList();

                // 4. إحصائيات المقارنة بين البوزيتف والنيجيتف (Column Chart)
                // تقدري تحددي الـ Positive والـ Negative بناءً على الـ Rating (مثلاً 4 و 5 بوزيتف، 1 و 2 نيجيتف)
                var positiveCount = allFeedback.Count(f => f.Rating >= 4);
                var negativeCount = allFeedback.Count(f => f.Rating <= 2);
                var neutralCount = allFeedback.Count(f => f.Rating == 3);

                var sentimentData = new[]
                {
            new { Sentiment = "Positive (4-5 ★)", Count = positiveCount },
            new { Sentiment = "Neutral (3 ★)", Count = neutralCount },
            new { Sentiment = "Negative (1-2 ★)", Count = negativeCount }
        };

                // الـ Response الموحد بالكامل
                return Ok(new
                {
                    status = "success",
                    data = new
                    {
                        totalFeedback = totalFeedback,
                        feedbackTypes = feedbackTypeData,
                        modelTargets = modelTargetData,
                        sentimentStats = sentimentData
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
    }
}