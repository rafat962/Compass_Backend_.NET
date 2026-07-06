using CompassAI.Models.Domain;
using CompassAI.Models.DTO;

namespace CompassAI.Repositories.Auth
{
    public interface IAuthRepository
    {

        Task<User?> RegisterAsync(User user);
        Task<User?> GetUserByIdAsync(Guid ID);
        Task<User?> UpdateUserAsync(User user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByResetToken(string resetToken);
    }
}
