using System.Security.Claims;
using CompassAI.Models.Domain;

namespace CompassAI.Services.Token
{
    public interface ITokenService
    {
        string CreateToken(User user);
        ClaimsPrincipal GetPrincipalFromToken(string token);
    }
}
