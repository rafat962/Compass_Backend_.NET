using CompassAI.Models.Domain;

namespace CompassAI.Repositories.Permission
{
    public interface IPermissionRepository
    {
        Task<List<UserPermission>> GetUserPermissionsAsync(Guid userId);
        Task AssignPermissionAsync(UserPermission permission);
        Task RevokeResourceAsync(Guid userId, string resource);
    }
}
