using CompassAI.Models.DTO;
using CompassAI.Repositories.APIKEY;
using CompassAI.Repositories.Permission;
using CompassAI.Repositories.Users;
using CompassAI.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        private async Task<(object UserData, object[] Permissions)> BuildUserDataAsync(CompassAI.Models.Domain.User user)
        {
            var userKeys = await _apiKeyRepository.GetByUserIdAsync(user.Id);
            var activeKey = userKeys.FirstOrDefault(key => key.IsActive);
            var permissions = await PermissionRepo.GetUserPermissionsAsync(user.Id);

            return (
                new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Photo,
                    user.CurrentPlan,
                    user.Role,
                    apiKey = activeKey?.Key
                },
                permissions.Select(permission => (object)new
                { 
                    Resource = permission.Resource,
                    Route = permission.Route,
                    RouteName = permission.RouteName,
                    Actions = permission.Actions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                }).ToArray()
            );
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userClaimId = User.FindFirst("sub")?.Value
                           ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userClaimId) || !Guid.TryParse(userClaimId, out var userId))
                return Unauthorized();

            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { status = "fail", message = "User not found" });

            var result = await BuildUserDataAsync(user);
            return Ok(new { status = "success", userData = result.UserData, perms = result.Permissions });
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
        // Add New User
        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] AddUserDto model)
        {
            try
            {
                // 1. Check if email is already taken
                var isTaken = await _userRepo.IsEmailTakenAsync(model.Email, Guid.Empty);
                if (isTaken)
                    return BadRequest(new { status = "error", message = "Email already taken" });
                // 2. Create new user
                var newUser = new Models.Domain.User
                {
                    Name = model.Name,
                    Email = model.Email,
                    Role = model.Role,
                    CurrentPlan = model.CurrentPlan,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("defaultPassword123") // Default password, should be changed later
                };
                await _userRepo.AddUserAsync(newUser);
                return Ok(new { status = "success", message = "User added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
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

                    user.Email = model.Email.Trim();
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

                var result = await BuildUserDataAsync(user);

                return Ok(new
                {
                    status = "success",
                    message = "Profile updated successfully",
                    // Keep `user` for existing consumers and `userData` for Settings.
                    user = result.UserData,
                    userData = result.UserData,
                    perms = result.Permissions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
        [HttpPut("admin/updateUser/{id}")]
        [Authorize(Roles = "Admin,admin")] // اختياري: لحماية الـ Route للـ Admins فقط
        public async Task<IActionResult> UpdateUserByAdmin(Guid id, [FromForm] UpdateUserDto model, IFormFile? file)
        {
            try
            {
                // بنجيب اليوزر اللي مبعوت الـ ID بتاعه من الـ URL مش من الـ Token!
                var user = await _userRepo.GetUserByIdAsync(id);

                if (user == null)
                    return NotFound(new { status = "fail", message = "User not found" });

                // تشيك تكرار الإيميل مع استثناء الـ ID بتاع اليوزر اللي بنعدله حالياً
                if (!string.IsNullOrEmpty(model.Email) && !string.Equals(model.Email.Trim(), user.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var isTaken = await _userRepo.IsEmailTakenAsync(model.Email, id);
                    if (isTaken)
                        return BadRequest(new { status = "error", message = "Email already taken" });

                    user.Email = model.Email.Trim();
                }

                if (!string.IsNullOrEmpty(model.Name)) user.Name = model.Name;
                if (!string.IsNullOrEmpty(model.Role)) user.Role = model.Role;
                if (!string.IsNullOrEmpty(model.CurrentPlan)) user.CurrentPlan = model.CurrentPlan;

                //// تعديل الصورة لو اترفع ملف جديد
                //if (file != null && file.Length > 0)
                //{
                //    if (!string.IsNullOrEmpty(user.Photo) && user.Photo != "none")
                //    {
                //        var oldPath = Path.Combine(_env.WebRootPath, "users", user.Photo);
                //        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                //    }

                //    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                //    var filePath = Path.Combine(_env.WebRootPath, "users", fileName);

                //    using (var stream = new FileStream(filePath, FileMode.Create))
                //    {
                //        await file.CopyToAsync(stream);
                //    }
                //    user.Photo = fileName;
                //}
                if (model.Active.HasValue)
                {
                    user.Active = model.Active.Value;
                }
                await _userRepo.UpdateUserAsync(user);

                return Ok(new { status = "success", message = "User updated successfully by admin" });
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
        //Admin DAshboard 
        [HttpGet("admin/dashboard-stats")]
        [Authorize] // أو [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                // 1. جلب كل المستخدمين في كويري واحدة لتوفير الوقت والضغط على السيرفر
                var users = await _userRepo.GetAllUsersAsync();

                // 2. حساب الأعداد الأساسية (الكروت)
                var totalUsers = users.Count();
                var activeUsers = users.Count(u => u.Active); 
                var blockedUsers = totalUsers - activeUsers;
                //var totalTokensUsed = await Content.UsageLogs.SumAsync(log => log.TokensUsed);

                // 3. تجهيز بيانات الـ Pie Chart (توزيع الباقات)
                var planCounts = new Dictionary<string, int>
                    {
                        { "Free", 0 },
                        { "Basic", 0 },
                        { "Gold", 0 },
                        { "Premium", 0 }
                    };

                foreach (var user in users)
                {
                    var plan = user.CurrentPlan ?? "Free"; // تأكدي من اسم الحقل
                    if (planCounts.ContainsKey(plan))
                    {
                        planCounts[plan]++;
                    }
                    else
                    {
                        planCounts["Free"]++;
                    }
                }

                var pieChartData = planCounts
                    .Select(p => new { Name = $"{p.Key} Plan", Value = p.Value })
                    .Where(p => p.Value > 0) // نظهر الباقات اللي فيها يوزرز بس
                    .ToList();

                // 4. تجهيز بيانات الـ Line Chart (معدل التسجيل في آخر 7 أيام)
                var lastWeekDate = DateTime.UtcNow.AddDays(-7);
                var recentUsers = users.Where(u => u.CreatedAt >= lastWeekDate).ToList(); // تأكدي من حقل الـ CreatedAt

                var lineChartData = Enumerable.Range(0, 7)
                    .Select(i => lastWeekDate.AddDays(i))
                    .Select(date => new
                    {
                        Name = date.ToString("ddd"), // مثل: Sat, Sun, Mon
                        Users = recentUsers.Count(u => u.CreatedAt.Date == date.Date)
                    })
                    .ToList();

                // compute token usage across all api keys (per-model counts are stored on ApiKey)
                var apiKeys = (await _apiKeyRepository.GetAllAsync()).ToList();
                var totalMapTalkTokens = apiKeys.Sum(k => k.MapTalkLimit);
                var totalSpecReviewerTokens = apiKeys.Sum(k => k.SpecReviewerLimit);
                var totalDocQueryTokens = apiKeys.Sum(k => k.DocQueryLimit);
                var totalTokensUsed = totalMapTalkTokens + totalSpecReviewerTokens + totalDocQueryTokens;

                // If no usage data exists yet, provide default/sample numbers so frontend charts render while developing
                if (totalTokensUsed == 0)
                {
                    totalMapTalkTokens = 100;      // sample default
                    totalSpecReviewerTokens = 800;  // sample default
                    totalDocQueryTokens = 400;      // sample default
                    totalTokensUsed = totalMapTalkTokens + totalSpecReviewerTokens + totalDocQueryTokens;
                }

                // 5. تجميع كل البيانات وإرجاعها في الـ Response
                return Ok(new
                {
                    status = "success",
                    data = new
                    {
                        cards = new
                        {
                            TotalUsers = totalUsers,
                            ActiveUsers = activeUsers,
                            BlockedUsers = blockedUsers,
                            SystemApiKeys = 18, // قيمة ثابتة مؤقتاً لمشروع Compass AI
                            Tokens = new {
                                Total = totalTokensUsed,
                                ByModel = new {
                                    MapTalk = totalMapTalkTokens,
                                    SpecReviewer = totalSpecReviewerTokens,
                                    DocQuery = totalDocQueryTokens
                                }
                            }
                        },
                        pieChart = pieChartData,
                        lineChart = lineChartData
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
        // 4 & 5. Toggle User Status (Activate/Block)
        [HttpPatch("UpdateUserStatus/{userId:Guid}")]
        public async Task<IActionResult> UpdateUserStatus(Guid userId, [FromQuery] bool status)
        {
            try
            {
                var user = await _userRepo.GetUserByIdAsync(userId);
                if (user == null)
                    return NotFound(new { status = "error", message = "User not found" });

                // تحديث حالة المستخدم بناءً على قيمة الـ status المبعوتة (true للـ Activate و false للـ Block)
                await _userRepo.UpdateUserActiveStatusAsync(userId, status, true);

                string message = status ? "activated" : "blocked";

                // لو العملية تفعيل، نرسل الإيميل بشكل آمن
                if (status)
                {
                    try
                    {
                        string loginUrl = "http://localhost:5173/auth/login";
                        await _emailService.SendWelcomeEmail(user, loginUrl);
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine($"[Email Service Error]: {mailEx.Message}");
                    }
                }

                return Ok(new
                {
                    status = "success",
                    message = $"User has been {message} successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
    }
}
