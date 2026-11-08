using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Infrastructure.Security.Keys;

namespace SubSnapMS.Identity.Infrastructure.Security.Tokens;

//Access token JWT firmato RS256 con la chiave attiva (header "kid" → i validatori scelgono la chiave giusta dal JWKS).
//Claims minimi: sub (userId), email, jti. Niente dati sensibili: un JWT è leggibile da chiunque lo abbia.
internal sealed class JwtAccessTokenIssuer(ISigningKeyStore signingKeys, IOptions<JwtOptions> options) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    public AccessToken Issue(User user, DateTimeOffset now)
    {
        var jwt = options.Value;
        var expiresAtUtc = now + jwt.AccessTokenLifetime;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAtUtc.UtcDateTime,
            SigningCredentials = signingKeys.GetSigningCredentials(),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.Value.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email.Value,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N")
            }
        };

        return new AccessToken(_handler.CreateToken(descriptor), expiresAtUtc);
    }
}