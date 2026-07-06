using System.Security.Claims;
using CompassAI.Models.DTO;
using CompassAI.Repositories.APIKEY;
using CompassAI.Repositories.Permission;
using CompassAI.Repositories.Users;
using CompassAI.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CompassAI.Controllers.UserControllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly IApikeyRepository _apiKeyRepository;

        public IPermissionRepository PermissionRepo { get; }

        public UserController(IUserRepository userRepo, IEmailService emailService, IWebHostEnvironment env, IApikeyRepository apiKeyRepository, IPermissionRepository PermissionRepo)
        {
            _userRepo = userRepo;
            _emailService = emailService;
            _env = env;
            _apiKeyRepository = apiKeyRepository;
            this.PermissionRepo = PermissionRepo;
        }

        // 1. Get Pending Users
        [HttpGet("getPendingUsers")]
        public async Task<IActionResult> GetPendingUsers()
        {
            try
            {
                var users = await _userRepo.GetPendingUsersAsync();
                return Ok(users); // Node.js كان بيرجع Array مباشرة
            }
            catch (Exception ex)
            {
                return NotFound(new { status = "error", message = ex.Message });
            }
        }

        // 2. Accept Pending Email
        [HttpPatch("acceptPendingEmail/{userId:Guid}")]
        public async Task<IActionResult> AcceptPendingEmail(Guid userId)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(userId);
                if (user == null) return NotFound(new { status = "error", message = "User not found" });

                await _userRepo.UpdateUserActiveStatusAsync(userId, true);

                return Ok(new
                {
                    status = "success",
                    message = $"User {user.Name} has been activated successfully."
                });
            }
            catch (Exception ex)
            {
                return NotFound(new { status = "error", message = ex.Message });
            }
        }

        // 3. Reject/Delete User
        [HttpDelete("rejectUser/{userId:Guid}")]
        public async Task<IActionResult> RejectUser(Guid userId)
        {
            try
            {
                await _userRepo.DeleteUserAsync(userId);
                return Ok(new { status = "success", message = "User has been deleted successfully" });
            }
            catch (Exception ex)
            {
                return NotFound(new { status = "error", message = ex.Message });
            }
        }

        // 4. Activate User + Send Email
        [HttpPatch("ActiviteUser/{userId:Guid}")]
        public async Task<IActionResult> ActivateUser(Guid userId)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(userId);
                if (user == null) return NotFound(new { status = "error", message = "User not found" });

                await _userRepo.UpdateUserActiveStatusAsync(userId, true, true);

                // هتبعث الـ user والـ URL بتاع اللوجين
                string loginUrl = "http://localhost:5173/auth/login";
                await _emailService.SendWelcomeEmail(user, loginUrl);

                return Ok(new { status = "success", message = "User has been activated successfully and email sent." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }

        // 5. Get User Logs
        [HttpGet("getUserLogs/{id:Guid}")]
        public async Task<IActionResult> GetUserLogs(Guid id)
        {
            try
            {
                var logs = await _userRepo.GetUserLogsAsync(id);
                return Ok(new { loginLogs = logs }); // يحاكي الـ select("loginLogs")
            }
            catch (Exception ex)
            {
                return NotFound(new { status = "error", message = ex.Message });
            }
        }


        [HttpGet("getAllUsers")]
        [Authorize]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userRepo.GetAllUsersAsync();

                // Node.js كان بيرجع المصفوفة مباشرة في الـ success
                return Ok(users);
            }
            catch (Exception ex)
            {
                // مطابقة لشكل الـ error في كود الـ Node.js بتاعك
                return NotFound(new
                {
                    status = "error",
                    message = ex.Message
                });
            }
        }

        [HttpPut("updateUser")]
        [Authorize]
        public async Task<IActionResult> UpdateUser([FromForm] UpdateUserDto model, IFormFile? file)
        {
            try
            {
                // 1. Get User ID from Token
                var userClaimId = User.FindFirst("sub")?.Value
                               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userClaimId)) return Unauthorized();

                var userId = Guid.Parse(userClaimId);
                var user = await _userRepo.GetUserByIdAsync(userId);

                if (user == null)
                    return NotFound(new { status = "fail", message = "User not found" });

                // 2. Email duplication check
                if (!string.IsNullOrEmpty(model.Email) && model.Email != user.Email)
                {
                    var isTaken = await _userRepo.IsEmailTakenAsync(model.Email, userId);
                    if (isTaken)
                        return BadRequest(new { status = "error", message = "Email already taken" });

                    user.Email = model.Email;
                }

                // 3. Update Name
                if (!string.IsNullOrEmpty(model.Name)) user.Name = model.Name;

                // 4. Handle Photo upload
                if (file != null && file.Length > 0)
                {
                    if (!string.IsNullOrEmpty(user.Photo) && user.Photo != "none")
                    {
                        var oldPath = Path.Combine(_env.WebRootPath, "users", user.Photo);
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                    }

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(_env.WebRootPath, "users", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    user.Photo = fileName;
                }

                // 5. Save Changes
                await _userRepo.UpdateUserAsync(user);

                // --- NEW: Fetch API Key and Permissions to match Login response ---

                // Fetch active API Key
                var userKeys = await _apiKeyRepository.GetByUserIdAsync(user.Id);
                var activeKey = userKeys.FirstOrDefault(k => k.IsActive);

                // Fetch and Format Permissions
                var perms = await PermissionRepo.GetUserPermissionsAsync(user.Id);
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
                    message = "Profile updated successfully",
                    userData = new
                    {
                        name = user.Name,
                        email = user.Email,
                        photo = user.Photo,
                        currentPlan = user.CurrentPlan,
                        apiKey = activeKey?.Key // Added API Key
                    },
                    perms = formattedPerms // Added Permissions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }


        [HttpPut("change-password")]
        [Authorize] 
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            try
            {
                // 1. Get User ID from Token (Identity)
                var userClaimId = User.FindFirst("sub")?.Value
                               ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userClaimId)) return Unauthorized();

                var userId = Guid.Parse(userClaimId);
                var user = await _userRepo.GetUserByIdAsync(userId);

                if (user == null) return NotFound(new { status = "fail", message = "User not found" });

                // 2. Verify Old Password
                bool isOldPasswordValid = BCrypt.Net.BCrypt.Verify(model.OldPassword, user.PasswordHash);
                if (!isOldPasswordValid)
                {
                    return BadRequest(new { status = "error", message = "The old password you entered is incorrect." });
                }

                // 3. Hash and Update New Password
                string salt = BCrypt.Net.BCrypt.GenerateSalt(8);
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, salt);

                await _userRepo.UpdateUserAsync(user);

                // --- Fetch API Key and Permissions (To keep the same response structure) ---
                var userKeys = await _apiKeyRepository.GetByUserIdAsync(user.Id);
                var activeKey = userKeys.FirstOrDefault(k => k.IsActive);

                var perms = await PermissionRepo.GetUserPermissionsAsync(user.Id);
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
                    message = "Password changed successfully",
                    userData = new
                    {
                        name = user.Name,
                        email = user.Email,
                        photo = user.Photo,
                        currentPlan = user.CurrentPlan,
                        apiKey = activeKey?.Key
                    },
                    perms = formattedPerms
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
        [HttpDelete("deleteImage")]
        [Authorize]
        public async Task<IActionResult> DeleteImage()
        {
            try
            {
                var userClaimId = User.FindFirst("sub")?.Value;
                var userId = Guid.Parse(userClaimId);
                var user = await _userRepo.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(new { status = "fail", message = "User not found" });
                if (!string.IsNullOrEmpty(user.Photo) && user.Photo != "none")
                {
                    var imagePath = Path.Combine(_env.WebRootPath, "users", user.Photo);
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    user.Photo = "none";
                    await _userRepo.UpdateUserAsync(user);
                }
                return Ok(new { status = "success", message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
    }
}
