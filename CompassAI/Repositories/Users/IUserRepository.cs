using CompassAI.Models.Domain;

namespace CompassAI.Repositories.Users
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetPendingUsersAsync();
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);
        Task AddUserAsync(User user);
        Task UpdateUserActiveStatusAsync(Guid id, bool isActive, bool emailActive = false);
        Task DeleteUserAsync(Guid id);
        Task<List<string>> GetUserLogsAsync(Guid id);
        Task UpdateUserAsync(User user);
        Task<bool> IsEmailTakenAsync(string email, Guid excludeUserId);

        Task<bool> UpdateUserPackage(ApiKey ApiKey);
    }
}
