using System.Security.Claims;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.BuildingBlocks.Web.Security;

public static class ClaimsPrincipalExtensions
{
    public const string SubjectClaim = "sub";
    public const string EmailClaim = "email";

    /// <summary>Id dell'utente dal claim "sub". Sostituisce il GetCurrentUserId() ripetuto nei controller del monolite.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        principal.TryGetUserId(out var userId)
            ? userId
            : throw new UnauthorizedException("auth.missing_subject", "The access token does not identify a user.");

    public static bool TryGetUserId(this ClaimsPrincipal principal, out Guid userId)
    {
        userId = Guid.Empty;
        var value = principal.FindFirst(SubjectClaim)?.Value;
        return value is not null && Guid.TryParse(value, out userId) && userId != Guid.Empty;
    }

    public static string? GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirst(EmailClaim)?.Value;
}