using CompassAI.Data;
using CompassAI.Models.Domain;
using CompassAI.Repositories.Permission;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Repositories.Implementation
{
    public class SQLPermissionRepository : IPermissionRepository
    {
        private readonly ApplicationDbContext _context;

        public SQLPermissionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<UserPermission>> GetUserPermissionsAsync(Guid userId)
        {
            return await _context.UserPermissions
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task AssignPermissionAsync(UserPermission permission)
        {
            var existing = await _context.UserPermissions
                .FirstOrDefaultAsync(p => p.UserId == permission.UserId && p.Route == permission.Route);

            if (existing != null)
            {
                existing.Actions = permission.Actions;
                existing.Resource = permission.Resource;
                existing.RouteName = permission.RouteName;
            }
            else
            {
                // إضافة صلاحية جديدة تماماً
                await _context.UserPermissions.AddAsync(permission);
            }

            await _context.SaveChangesAsync();
        }

        public async Task RevokeResourceAsync(Guid userId, string resource)
        {
            var items = _context.UserPermissions
                .Where(p => p.UserId == userId && p.Resource == resource);

            if (items.Any())
            {
                _context.UserPermissions.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
        }
    }
}