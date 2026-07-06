using CompassAI.Models.Domain;
using CompassAI.Repositories.Permission;
using CompassAI.Services.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompassAI.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly IPermissionRepository _permissionRepo;
        private readonly ITokenService tokenService;

        public PermissionController(IPermissionRepository permissionRepo, ITokenService tokenService)
        {
            _permissionRepo = permissionRepo;
            this.tokenService = tokenService;
        }

        // Get permissions for a specific user
        [HttpGet("user-permissions")] // Removed {userId} from route
        [Authorize]
        public async Task<IActionResult> GetUserPermissions()
        {
            // Extract userId from the token claims
            // Use "sub" or ClaimTypes.NameIdentifier based on your token configuration
            var userIdClaim = User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "User ID not found in token" });
            }

            var userId = Guid.Parse(userIdClaim);
            var perms = await _permissionRepo.GetUserPermissionsAsync(userId);

            var formattedPerms = perms.Select(p => new
            {
                Resource = p.Resource,
                Route = p.Route,
                RouteName = p.RouteName,
                Actions = p.Actions.Split(',', StringSplitOptions.RemoveEmptyEntries)
            });

            return Ok(new
            {
                status = "success",
                perms = formattedPerms
            });
        }
        // Assign or update user permissions
        [HttpPost("assign")]
        public async Task<IActionResult> AssignPermission([FromBody] AssignPermissionDto dto)
        {
            var permission = new UserPermission
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Resource = dto.Resource,
                Route = dto.Route,
                RouteName = dto.RouteName,
                Actions = dto.Actions // Expected: "ADD,DELETE,VIEW"
            };

            await _permissionRepo.AssignPermissionAsync(permission);

            return Ok(new { status = "success", message = "Permission assigned successfully" });
        }
    }

    // Data transfer object for assigning permissions
    public class AssignPermissionDto
    {
        public Guid UserId { get; set; }
        public string Resource { get; set; }
        public string Route { get; set; }
        public string RouteName { get; set; }
        public string Actions { get; set; }
    }
}