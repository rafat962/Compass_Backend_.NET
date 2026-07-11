using System.Collections.Generic;
using System.Threading.Tasks;
using CompassAI.Models;
using CompassAI.Models.Enums;

namespace CompassAI.Repositories
{
    public interface IModelFeedbackRepository
    {
        Task<ModelFeedback> AddAsync(ModelFeedback feedback);
        Task<IEnumerable<ModelFeedback>> GetAllAsync(ModelType? modelTarget = null, FeedbackType? type = null);
        Task<IEnumerable<ModelFeedback>> GetByUserIdAsync(Guid userId);
        Task<ModelFeedback?> GetByIdAsync(int id);
        Task<bool> DeleteAsync(int id);
    }
}