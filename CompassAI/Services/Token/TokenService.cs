using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CompassAI.Models.Domain;
using CompassAI.Services.Token;
using Microsoft.IdentityModel.Tokens;

public class TokenService : ITokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;

    public string CreateToken(User user)
    {
        // Ensure map is clear
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        var claims = new List<Claim> {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = creds,
            Issuer = _config["JWT:Issuer"],
            // REMOVE OR FIX THIS LINE:
            // If you want to use it, it must match the key in appsettings exactly
            // Audience = _config["JWT:Audience"] 
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
    public ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        // تنظيف الـ Map
        tokenHandler.InboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        var keySection = _config["JWT:Key"];
        var issuerSection = _config["JWT:Issuer"];

        if (string.IsNullOrEmpty(keySection))
        {
            throw new ArgumentNullException("JWT:Key", "JWT Key is not configured");
        }

        var key = Encoding.UTF8.GetBytes(keySection);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),

                ValidateIssuer = true,
                ValidIssuer = issuerSection,

                ValidateAudience = false, // تغيير إلى true إذا أضفت Audience

                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // أعط مهلة 5 دقائق
            }, out SecurityToken validatedToken);

            return principal;
        }
        catch (Exception ex)
        {
            if (ex is SecurityTokenExpiredException)
                throw new Exception("Token has expired");
            else if (ex is SecurityTokenInvalidSignatureException)
                throw new Exception("Invalid token signature");
            else
                throw new Exception($"Token validation failed: {ex.Message}");
        }
    }
}