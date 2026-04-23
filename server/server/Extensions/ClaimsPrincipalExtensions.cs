using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace server.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        var userIdValue =
            claimsPrincipal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }
}
