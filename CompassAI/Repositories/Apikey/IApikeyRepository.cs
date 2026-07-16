using CompassAI.Models.Domain;

namespace CompassAI.Repositories.APIKEY
{
    public interface IApikeyRepository
    {
        // Retrieve all keys associated with a specific user
        Task<IEnumerable<ApiKey>> GetByUserIdAsync(Guid userId);

        // Find a specific key by its unique database identifier
        Task<ApiKey?> GetByIdAsync(Guid id);

        // Fetch key details using the actual string value for validation
        Task<ApiKey?> GetByKeyStringAsync(string key);

        // Persist a newly generated API key to the database
        Task<ApiKey> CreateAsync(ApiKey apiKey);

        // Update existing key metadata or status
        Task<ApiKey> UpdateAsync(ApiKey apiKey);

        // Remove an API key permanently from the system
        Task<bool> DeleteAsync(Guid id);

        // Check if the key is active and has remaining request balance
        Task<bool> HasValidQuotaAsync(string key);

        // Increment the usage counter after a successful API request
        Task<bool> RecordUsageAsync(string key,string model);

        // Retrieve all api keys (used for reporting/aggregation)
        Task<IEnumerable<ApiKey>> GetAllAsync();
    }
}