using CompassAI.Data;
using CompassAI.Models.Domain;
using CompassAI.Models.DTO;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Repositories.Auth
{
    public class SQLAuthRepository : IAuthRepository
    {
        public SQLAuthRepository(ApplicationDbContext context)
        {
            Context = context;
        }

        public ApplicationDbContext Context { get; }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            var user = await Context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return null;
            }
            return user;
        }

        public Task<User?> GetUserByIdAsync(Guid ID)
        {
            return Context.Users.FirstOrDefaultAsync(u => u.Id == ID);

        }

        public Task<User?> GetUserByResetToken(string resetToken)
        {
            return Context.Users.FirstOrDefaultAsync(u => u.ResetPasswordToken == resetToken);
        }

        public async Task<User?> RegisterAsync(User newUser)
        {
            var existingUser = await Context.Users.FirstOrDefaultAsync(u => u.Email == newUser.Email);
            if (existingUser != null) return null;

            await Context.Users.AddAsync(newUser);
            await Context.SaveChangesAsync();

            return newUser;
        }

        public Task<User?> UpdateUserAsync(User user)
        {
            Context.Users.Update(user);
            return Context.SaveChangesAsync().ContinueWith(t => user);
        }
    }
}
