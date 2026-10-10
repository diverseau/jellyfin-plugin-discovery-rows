using System.Security.Claims;

namespace Jellyfin.Plugin.DiscoveryRows.Services;

public static class RequestIdentity
{
    public static Guid UserId(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true) return Guid.Empty;
        return Guid.TryParse(principal.FindFirst("Jellyfin-UserId")?.Value, out var id) ? id : Guid.Empty;
    }
    public static bool Matches(ClaimsPrincipal? principal, Guid requested) => requested != Guid.Empty && UserId(principal) == requested;
}
