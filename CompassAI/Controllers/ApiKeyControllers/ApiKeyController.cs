using System.Security.Cryptography;
using CompassAI.Models.Domain;
using CompassAI.Repositories.APIKEY;
using CompassAI.Repositories.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CompassAI.Controllers.ApiKeyControllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ApiKeyController : ControllerBase
    {
        private readonly IApikeyRepository _apiKeyRepository;
        private readonly IAuthRepository _authRepository;

        public ApiKeyController(IApikeyRepository apiKeyRepository, IAuthRepository authRepository)
        {
            _apiKeyRepository = apiKeyRepository;
            _authRepository = authRepository;
        }

        // Generate a new secure API Key with 1-month default expiry
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] CreateKeyRequestDto request)
        {
            int calculatedLimit = request.PackageType.ToLower() switch
            {
                "free" => 50,
                "basic" => 300,
                "gold" => 1000,
                "premium" => 10000,
                _ => 50
            };
            // Create a secure random string with prefix
            var keyString = $"cmp_{Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLower()}";

            var apiKey = new ApiKey
            {
                Id = Guid.NewGuid(),
                Key = keyString,
                UserId = request.UserId,
                PackageType = request.PackageType,
                RequestsLimit = calculatedLimit,
                RequestsUsed = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMonths(1) 
            };

            await _apiKeyRepository.CreateAsync(apiKey);
            return Ok(apiKey);
        }

        // Get all keys belonging to a specific user
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetUserKeys(Guid userId)
        {
            var keys = await _apiKeyRepository.GetByUserIdAsync(userId);
            return Ok(keys);
        }

        // Toggle key status (Activate/Deactivate)
        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] bool isActive)
        {
            var apiKey = await _apiKeyRepository.GetByIdAsync(id);
            if (apiKey == null) return NotFound();

            apiKey.IsActive = isActive;
            await _apiKeyRepository.UpdateAsync(apiKey);

            return Ok(apiKey);
        }

        // Delete an API key permanently
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _apiKeyRepository.DeleteAsync(id);
            if (!deleted) return NotFound();

            return NoContent();
        }

        // Validate a key and check remaining quota
        [HttpGet("validate/{key}")]
        public async Task<IActionResult> Validate(string key)
        {
            var apiKey = await _apiKeyRepository.GetByKeyStringAsync(key);
            if (apiKey == null) return NotFound("Invalid Key");

            return Ok(new
            {
                isValid = apiKey.IsValid,
                remainingRequests = apiKey.RequestsLimit - apiKey.RequestsUsed,
                package = apiKey.PackageType,
                expiry = apiKey.ExpiresAt
            });
        }

        // Reset usage and update limits for package renewal
        [HttpPut("renew/{key}")]
        public async Task<IActionResult> RenewPackage(string key, [FromBody] RenewPackageDto dto)
        {
            var apiKey = await _apiKeyRepository.GetByKeyStringAsync(key);
            if (apiKey == null)
                return NotFound(new { status = "error", message = "API Key not found." });


            int calculatedLimit = dto.NewPackageType.ToLower() switch
            {
                "free" => 50,
                "basic" => 300,
                "gold" => 1000,
                "premium" => 10000,
                _ => 50
            };

            apiKey.PackageType = dto.NewPackageType;
            apiKey.RequestsLimit = calculatedLimit;
            apiKey.RequestsUsed = 0; 
            apiKey.ExpiresAt = DateTime.UtcNow.AddMonths(1); 

            await _apiKeyRepository.UpdateAsync(apiKey);
            return Ok(apiKey);
        }


        // Consume 1 request from the API key quota if valid
        [HttpPost("consume/{key}")]
        public async Task<IActionResult> ConsumeKey(string key)
        {
            // 1. Fetch the API Key
            var apiKey = await _apiKeyRepository.GetByKeyStringAsync(key);
            if (apiKey == null)
                return NotFound(new { status = "error", message = "Invalid API Key." });

            // 2. Check Activation Status
            if (!apiKey.IsActive)
                return BadRequest(new { status = "error", message = "This API Key has been deactivated." });

            // 3. Check Expiry Date (Time validation)
            bool isExpired = apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow;
            if (isExpired)
            {
                return BadRequest(new { status = "error", message = "API Key has expired for this month. Please renew your package." });
            }

            // 4. Check Quota Limit
            if (apiKey.RequestsUsed >= apiKey.RequestsLimit)
            {
                return BadRequest(new { status = "error", message = "You have exceeded your package requests limit for this month." });
            }

            // 5. Deduct quota (Increment consumption by 1)
            apiKey.RequestsUsed += 1;

            // 6. Save changes via Repository
            await _apiKeyRepository.UpdateAsync(apiKey);

            return Ok(new
            {
                status = "success",
                message = "Request consumed successfully.",
                remainingRequests = apiKey.RequestsLimit - apiKey.RequestsUsed,
                package = apiKey.PackageType
            });
        }


        // Admin only: Update credits, expiry, and package type using API Key string
        [HttpPut("admin/update-key-credits/{key}")]
        public async Task<IActionResult> AdminUpdateKeyCredits(string key, [FromBody] AdminUpdateKeyDto dto)
        {
            // 1. Fetch the API Key
            var apiKey = await _apiKeyRepository.GetByKeyStringAsync(key);
            if (apiKey == null) return NotFound(new { status = "error", message = "API Key not found." });

            // 2. Update API Key properties
            apiKey.RequestsLimit += dto.ExtraRequests;

            // Update package type if provided
            if (!string.IsNullOrEmpty(dto.NewPackageType))
            {
                apiKey.PackageType = dto.NewPackageType;
            }

            // Extend expiry date
            var baseDate = (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt > DateTime.UtcNow)
                           ? apiKey.ExpiresAt.Value
                           : DateTime.UtcNow;
            apiKey.ExpiresAt = baseDate.AddDays(dto.ExtraDays);

            // 3. Update the User's package (Optional but recommended for consistency)
            // Assuming you have IUserRepository injected as _userRepository
            var user = await _authRepository.GetUserByIdAsync(apiKey.UserId);
            if (user != null && !string.IsNullOrEmpty(dto.NewPackageType))
            {
                // If your User model has a Package property, update it here
                // user.CurrentPackage = dto.NewPackageType; 
                await _authRepository.UpdateUserAsync(user);
            }

            // 4. Save changes via Repository
            await _apiKeyRepository.UpdateAsync(apiKey);

            return Ok(new
            {
                message = "API Key and Package updated successfully.",
                key = apiKey.Key,
                newPackage = apiKey.PackageType,
                newTotalLimit = apiKey.RequestsLimit,
                newExpiry = apiKey.ExpiresAt
            });
        }
    }

    #region DTOs
    public class CreateKeyRequestDto
    {
        public Guid UserId { get; set; }
        public string PackageType { get; set; } = "Free";
        //public int RequestsLimit { get; set; }
    }

    public class RenewPackageDto
    {
        public string NewPackageType { get; set; } = string.Empty;
        //public int NewLimit { get; set; }
    }
    public class AdminUpdateKeyDto
    {
        public int ExtraRequests { get; set; }
        public int ExtraDays { get; set; }
        public string? NewPackageType { get; set; } // Added for package updates
    }
    #endregion
}