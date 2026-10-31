using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SubSnapMS.BuildingBlocks.Web.Security;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;

    public Guid UserId => Principal.GetUserId();

    public string? Email => Principal.GetEmail();
}