using SubSnapMS.Identity.Application.Common.Sessions;

namespace SubSnapMS.Identity.API.Endpoints.Common;

//Risposta di login, refresh e cambio password. accessToken e refreshToken come nel old monolith SubSnap; le scadenze permettono al frontend di impostare maxAge dei cookie senza valori scritti a mano.
public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt)
{
    public static AuthTokensResponse From(AuthTokensResult result, DateTimeOffset now) => new(
        result.AccessToken,
        result.RefreshToken,
        "Bearer",
        (int)Math.Max(0, (result.AccessTokenExpiresAtUtc - now).TotalSeconds),
        result.AccessTokenExpiresAtUtc,
        result.RefreshTokenExpiresAtUtc);
}