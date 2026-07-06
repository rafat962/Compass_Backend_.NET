using CompassAI.Data;
using CompassAI.Models.Domain;
using CompassAI.Repositories.APIKEY;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Repositories.Apikey
{
    public class SQLApikeyRepository : IApikeyRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public SQLApikeyRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Get all keys for a user ordered by newest
        public async Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.ApiKeys
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        // Retrieve a single key by its Guid ID
        public async Task<ApiKey?> GetByIdAsync(Guid id)
        {
            return await _dbContext.ApiKeys.FindAsync(id);
        }

        // Find key by the actual string value
        public async Task<ApiKey?> GetByKeyStringAsync(string key)
        {
            return await _dbContext.ApiKeys
                .FirstOrDefaultAsync(x => x.Key == key);
        }

        // Add a new key to the database
        public async Task<ApiKey> CreateAsync(ApiKey apiKey)
        {
            await _dbContext.ApiKeys.AddAsync(apiKey);
            await _dbContext.SaveChangesAsync();
            return apiKey;
        }

        // Update key properties like name or status
        public async Task<ApiKey> UpdateAsync(ApiKey apiKey)
        {
            _dbContext.Entry(apiKey).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            return apiKey;
        }

        // Remove a key from the database
        public async Task<bool> DeleteAsync(Guid id)
        {
            var key = await _dbContext.ApiKeys.FindAsync(id);
            if (key == null) return false;

            _dbContext.ApiKeys.Remove(key);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        // Verify if the key exists, is active, and has remaining quota
        public async Task<bool> HasValidQuotaAsync(string key)
        {
            var apiKey = await _dbContext.ApiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == key);

            return apiKey != null && apiKey.IsValid;
        }

        // Increment the usage counter for the specific key
        public async Task<bool> RecordUsageAsync(string key)
        {
            var apiKey = await _dbContext.ApiKeys
                .FirstOrDefaultAsync(x => x.Key == key);

            if (apiKey == null || !apiKey.IsValid)
                return false;

            apiKey.RequestsUsed++;
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}