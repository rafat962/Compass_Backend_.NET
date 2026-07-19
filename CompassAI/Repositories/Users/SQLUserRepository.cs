using System;
using CompassAI.Data;
using CompassAI.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Repositories.Users
{
    public class SQLUserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        public SQLUserRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<User>> GetPendingUsersAsync() =>
            await _context.Users.Where(u => !u.EmailActive && !u.Active).ToListAsync();

        public async Task<IEnumerable<User>> GetAllUsersAsync() =>
            await _context.Users.ToListAsync();

        public async Task<User?> GetUserByIdAsync(Guid id) =>
            await _context.Users.FindAsync(id);

        public async Task UpdateUserActiveStatusAsync(Guid id, bool isActive, bool emailActive = false)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Active = isActive;
                user.EmailActive = true;
                if (emailActive) user.EmailActive = true;

                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }

        public Task<List<string>> GetUserLogsAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        //public async Task<List<string>> GetUserLogsAsync(string id)
        //{
        //    var user = await _context.Users.Include(u => u.LoginLogs)
        //                                 .FirstOrDefaultAsync(u => u.Id == id);
        //    return user?.LoginLogs ?? new List<LoginLog>();
        //}
        public async Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsEmailTakenAsync(string email, Guid excludeUserId) =>
            await _context.Users.AnyAsync(u => u.Email == email.ToLower().Trim() && u.Id != excludeUserId);

        public Task<bool> UpdateUserPackage(ApiKey ApiKey)
        {
            User user = _context.Users.FirstOrDefault(u => u.ApiKeys[0] == ApiKey);

            if(user == null)
            {
                return Task.FromResult(false);
            }
            user.CurrentPlan = ApiKey.PackageType;

            return Task.FromResult(true);
        }

        public async Task AddUserAsync(User user)
        {
             _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }
    }
}
