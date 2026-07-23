using System.Security.Claims;
using CompassAI.Data;
using CompassAI.Models.Domain;
using CompassAI.Models.DTO;
using CompassAI.Repositories.APIKEY;
using CompassAI.Repositories.Auth;
using CompassAI.Repositories.Permission;
using CompassAI.Services.Email;
using CompassAI.Services.Token;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompassAI.Controllers.AuthControllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        public AuthController(IAuthRepository authRepository,
            IEmailService emailService,
            ITokenService tokenService,
            IConfiguration config,
            IPermissionRepository PermissionRepo,
            IApikeyRepository apiKeyRepository) // Inject here
        {
            AuthRepository = authRepository;
            EmailService = emailService;
            TokenService = tokenService;
            Config = config;
            this.PermissionRepo = PermissionRepo;
            _apiKeyRepository = apiKeyRepository; // Initialize
        }

        public IAuthRepository AuthRepository { get; }
        public IEmailService EmailService { get; }
        public ITokenService TokenService { get; }
        public IConfiguration Config { get; }
        public IPermissionRepository PermissionRepo { get; }

        private IApikeyRepository _apiKeyRepository;


        // signUp
        [HttpPost("signup")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto registerRequest)
        {
            // 1 - Create OTP

            var otp = new Random().Next(100000, 999999).ToString();

            // 2 - Password Hashing
            string slat = BCrypt.Net.BCrypt.GenerateSalt(8);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password, slat);

            // 3 - Create new user
            // Mapping DTO to Domain Model
            var userDomainModel = new User
            {
                Name = registerRequest.Name,
                Email = registerRequest.Email,
                PasswordHash = hashedPassword,
                OTP = otp,
                Role = "user",
                Active = false
            };

            var result = await AuthRepository.RegisterAsync(userDomainModel);

            if (result == null)
            {
                return BadRequest(new
                {
                    status = "error",
                    message="User Already Exist"
                });
            }
            // --- NEW: Generate Default API Key for New User ---
            var defaultKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = $"cmp_{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16)).ToLower()}",
                UserId = result.Id,
                PackageType = "Free",
                RequestsLimit = 100, // Default limit for free tier
                RequestsUsed = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(1)
            };
            await _apiKeyRepository.CreateAsync(defaultKey);

            // 4 Generate Token

            var token = TokenService.CreateToken(result);

            // 5 - Send OTP Email
            // Prepare URL for the email
            var verifyUrl = $"{Config["Audience:Domain"]}/auth/active/{token}";

            // Now this should work after updating IEmailService
            await EmailService.SendOtpEmailAsync(result, verifyUrl);


            // Finally add user to database

            return Ok(new
            {
                message = "User registered successfully",
                token = token,
                user = new { result.Name, result.Email }
            });

        }

        // Active Account

        [HttpPost("active")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyOtpDto otpVerifyDto)
        {
            // 1 - Get User from Token
            var principal = TokenService.GetPrincipalFromToken(otpVerifyDto.token);

            if (principal == null)
            {
                return Unauthorized(new { status = "error", message = "Invalid or expired token" });
            }
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? principal.FindFirst("nameid")?.Value
                 ?? principal.FindFirst("sub")?.Value;
            // get User from database
            var user = await AuthRepository.GetUserByIdAsync(Guid.Parse(userId));
            // 1 - Validate Token
            if (user == null)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Invalid token"
                });
            }
            // 2 - Verify OTP
            if (user.OTP != otpVerifyDto.OTP)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Invalid OTP"
                });
            }
            // 3 - Activate User Account
            user.EmailActive = true;
            user.Active = true;
            user.OTP = null; // Clear OTP after successful verification
            await AuthRepository.UpdateUserAsync(user);
            return Ok(new
            {
                status = "success",
                message = "Email verified successfully"
            });
        }

        // lOGIN 
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginRequest)
        {
            // 1 - Get User by Email
            var user = await AuthRepository.GetUserByEmailAsync(loginRequest.Email);
            if (user == null)
            {
                return BadRequest(new { status = "error", message = "Invalid email or password" });
            }

            if (!user.EmailActive)
            {
                return BadRequest(new { status = "error", message = "Please verify your email before logging in" });
            }

            if (!user.Active)
            {
                return BadRequest(new { status = "error", message = "Your account has been deactivated." });
            }

            // 2 - Verify Password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return BadRequest(new { status = "error", message = "Invalid email or password" });
            }

            // Update Login Logs
            user.LoginLogs.Add(DateTime.Now);
            await AuthRepository.UpdateUserAsync(user);

            // 3 - NEW: Get User's API Key
            // بنجيب مفاتيح المستخدم وبناخد أول واحد نشط (أو أحدث واحد)
            var userKeys = await _apiKeyRepository.GetByUserIdAsync(user.Id);
            var activeKey = userKeys.FirstOrDefault(k => k.IsActive);

            // 4 - Generate Token & Permissions
            var token = TokenService.CreateToken(user);
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
                message = "Login successful",
                token = token,
                // بنضيف الـ ApiKey هنا في الرد
                // as
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Photo,
                    user.CurrentPlan,
                    user.Role,
                    apiKey = activeKey?.Key
                },
                perms = formattedPerms
            });
        }
        // reset Password Logic

        [HttpPost("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] resetPasswordDto resetPasswordDto)
        {
            // 1 - Get User by Email
            var user = await AuthRepository.GetUserByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "User with the provided email does not exist"
                });
            }
            // 2 - Set reset Token and Expiry
            var resetToken = Guid.NewGuid().ToString();
            user.ResetPasswordToken = resetToken;
            user.ResetPasswordExpires = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour
            await AuthRepository.UpdateUserAsync(user);
            // 3 - Send Reset Email
            var resetUrl = $"{Config["Audience:Domain"]}/auth/resetPassword/{resetToken}";
            await EmailService.SendPasswordResetEmailAsync(user, resetUrl);
            return Ok(new
            {
                status = "success",
                message = "Password reset email sent successfully"
            });
        }

        // Additional methods like actual password reset using the token can be added here

        [HttpPost("UpdatePassword")]
        public async Task<IActionResult> ResetPasswordConfirm([FromBody] NewPasswordDto newPasswordDto)
        {
            // 1 - Get User by Reset Token
            var user = await AuthRepository.GetUserByResetToken(newPasswordDto.token);

            if (user == null || user.ResetPasswordExpires == null || user.ResetPasswordExpires < DateTime.UtcNow)
            {
                return BadRequest(new
                {
                    status = "error",
                    message = "Invalid or expired reset token"
                });
            }

            // 2 - Hash New Password
            string slat = BCrypt.Net.BCrypt.GenerateSalt(8);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPasswordDto.NewPassword, slat);
            // 3 - Update User Password
            user.PasswordHash = hashedPassword;
            user.ResetPasswordToken = null;
            user.ResetPasswordExpires = null;
            await AuthRepository.UpdateUserAsync(user);
            return Ok(new
            {
                status = "success",
                message = "Password has been reset successfully"
            });

        }


        [HttpPost("token")]
        public async Task<IActionResult> VerifyToken([FromBody] TokenRequestDto request)
        {
            try
            {
                // 1. التأكد من وجود التوكن
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { status = "fail", message = "There is no token" });
                }

                // 2. فك التوكن واستخراج الـ Claims (باستخدام السيرفيس اللي عملناها)
                var principal = TokenService.GetPrincipalFromToken(request.Token);

                if (principal == null)
                {
                    return BadRequest(new { status = "fail", message = "INVALID" });
                }

                // 3. استخراج الـ ID من التوكن
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal.FindFirst("sub")?.Value          
                     ?? principal.FindFirst("nameid")?.Value;

                // 4. البحث عن اليوزر في قاعدة البيانات
                var user = await AuthRepository.GetUserByIdAsync(Guid.Parse(userId));

                if (user == null)
                {
                    return BadRequest(new { status = "fail", message = "User not found" });
                }

                // 5. التأكد إن الحساب نشط (زي ما كنت عامل في Node.js)
                if (!user.Active)
                {
                    return BadRequest(new { status = "fail", message = "User don't activated" });
                }

                // 6. الرد ببيانات اليوزر في حالة النجاح
                return Ok(new
                {
                    status = "success",
                    message = "VALID",
                    userData = new
                    {
                        name = user.Name,
                        email = user.Email,
                        role = user.Role,
                        currentPlan = user.CurrentPlan,
                        photo = user.Photo
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "fail", message = ex.Message });
            }
        }


        [HttpGet("my-keys")]
        [Authorize] // Requires login
        public async Task<IActionResult> GetMyKeys()
        {
            var userIdClaim = User.FindFirst("sub")?.Value;
            if (userIdClaim == null) return Unauthorized();

            var keys = await _apiKeyRepository.GetByUserIdAsync(Guid.Parse(userIdClaim));
            return Ok(keys);
        }
        public class TokenRequestDto
        {
            public string Token { get; set; }
        }
        [HttpPost("me")]
        public async Task<IActionResult> GetCurrentUserProfile([FromBody] TokenRequestDto request)
        {
            try
            {
                // 1. التأكد من وجود التوكن في الطلب
                if (string.IsNullOrEmpty(request.Token))
                {
                    return BadRequest(new { status = "error", message = "Token is required" });
                }

                // 2. فك التوكن واستخراج الـ Claims
                var principal = TokenService.GetPrincipalFromToken(request.Token);
                if (principal == null)
                {
                    return BadRequest(new { status = "error", message = "Invalid or expired token" });
                }

                // 3. استخراج الـ ID من الـ Claims
                var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? principal.FindFirst("sub")?.Value
                             ?? principal.FindFirst("nameid")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return BadRequest(new { status = "error", message = "User ID not found in token claims" });
                }

                var userGuid = Guid.Parse(userId);

                // 4. جلب بيانات اليوزر طازة من الداتا بيز
                var user = await AuthRepository.GetUserByIdAsync(userGuid);
                if (user == null)
                {
                    return BadRequest(new { status = "error", message = "User no longer exists" });
                }

                if (!user.Active)
                {
                    return BadRequest(new { status = "error", message = "Your account has been deactivated" });
                }

                // 5. جلب الـ API Key النشط للمستخدم (زي الـ Login بالظبط عشان يظهر في صفحة النجاح)
                var userKeys = await _apiKeyRepository.GetByUserIdAsync(user.Id);
                var activeKey = userKeys.FirstOrDefault(k => k.IsActive);

                // 6. جلب الـ Permissions وتنسيقها بنفس شكل الـ Login
                var perms = await PermissionRepo.GetUserPermissionsAsync(user.Id);
                var formattedPerms = perms.Select(p => new
                {
                    Resource = p.Resource,
                    Route = p.Route,
                    RouteName = p.RouteName,
                    Actions = p.Actions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                });

                // 7. الرد بنفس الـ Structure المتطابق مع الـ Login 100%
                return Ok(new
                {
                    status = "success",
                    message = "Profile fetched successfully",
                    token = request.Token, // إرجاع نفس التوكن
                    user = new
                    {
                        user.Id,
                        user.Name,
                        user.Email,
                        user.Photo,
                        user.CurrentPlan,
                        apiKey = activeKey?.Key // الـ API Key المحدث بعد الدفع
                    },
                    perms = formattedPerms
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = "error", message = ex.Message });
            }
        }
    }


}
