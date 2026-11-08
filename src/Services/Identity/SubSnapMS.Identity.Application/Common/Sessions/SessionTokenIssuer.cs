using Microsoft.Extensions.Options;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Application.Options;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Common.Sessions;

//Apre e ruota le sessioni: genera il refresh token, aggiorna l'aggregate, emette l'access token
public sealed class SessionTokenIssuer(
    IRefreshTokenFactory refreshTokenFactory,
    IAccessTokenIssuer accessTokenIssuer,
    IOptions<SessionOptions> options)
{
    //Nuova sessione (login, cambio password)
    public AuthTokensResult StartSession(User user, DateTimeOffset now)
    {
        var refreshToken = refreshTokenFactory.Generate();
        var refreshExpiresAtUtc = now + options.Value.RefreshTokenLifetime;

        user.SignIn(refreshToken.Hash, refreshExpiresAtUtc, now);

        return BuildTokens(user, refreshToken.Token, refreshExpiresAtUtc, now);
    }

    //Rotazione del refresh token presentato dal client
    public RefreshSessionResult RotateSession(User user, string presentedTokenHash, DateTimeOffset now)
    {
        var refreshToken = refreshTokenFactory.Generate();
        var refreshExpiresAtUtc = now + options.Value.RefreshTokenLifetime;

        var rotation = user.RotateRefreshToken(
            presentedTokenHash,
            refreshToken.Hash,
            refreshExpiresAtUtc,
            options.Value.ReuseGracePeriod,
            now);

        return rotation.Succeeded
            ? RefreshSessionResult.Success(BuildTokens(user, refreshToken.Token, refreshExpiresAtUtc, now))
            : RefreshSessionResult.Failure(rotation.Status);
    }

    private AuthTokensResult BuildTokens(User user, string refreshToken, DateTimeOffset refreshExpiresAtUtc, DateTimeOffset now)
    {
        var accessToken = accessTokenIssuer.Issue(user, now);
        return new AuthTokensResult(accessToken.Value, accessToken.ExpiresAtUtc, refreshToken, refreshExpiresAtUtc);
    }
}