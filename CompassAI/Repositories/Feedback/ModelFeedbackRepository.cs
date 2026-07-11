using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompassAI.Data;
using CompassAI.Models;
using CompassAI.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Repositories
{
    public class ModelFeedbackRepository : IModelFeedbackRepository
    {
        private readonly ApplicationDbContext _context;

        public ModelFeedbackRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ModelFeedback> AddAsync(ModelFeedback feedback)
        {
            await _context.ModelFeedbacks.AddAsync(feedback);
            await _context.SaveChangesAsync();
            return feedback;
        }

        public async Task<IEnumerable<ModelFeedback>> GetAllAsync(ModelType? modelTarget = null, FeedbackType? type = null)
        {
            var query = _context.ModelFeedbacks
                .Include(f => f.User) // تضمين بيانات المستخدم للعرض
                .AsQueryable();

            if (modelTarget.HasValue)
                query = query.Where(f => f.ModelTarget == modelTarget.Value);

            if (type.HasValue)
                query = query.Where(f => f.Type == type.Value);

            return await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
        }

        public async Task<IEnumerable<ModelFeedback>> GetByUserIdAsync(Guid userId)
        {
            return await _context.ModelFeedbacks
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
        public async Task<ModelFeedback?> GetByIdAsync(int id)
        {
            return await _context.ModelFeedbacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var feedback = await _context.ModelFeedbacks.FindAsync(id);
            if (feedback == null) return false;

            _context.ModelFeedbacks.Remove(feedback);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}