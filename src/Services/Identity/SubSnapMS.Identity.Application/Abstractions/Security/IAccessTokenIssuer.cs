using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Abstractions.Security;

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc)
{
    public override string ToString() => $"AccessToken {{ ExpiresAtUtc = {ExpiresAtUtc:O} }}";
}

//Emette il JWT firmato (RS256). Implementato in Infrastructure
public interface IAccessTokenIssuer
{
    AccessToken Issue(User user, DateTimeOffset now);
}