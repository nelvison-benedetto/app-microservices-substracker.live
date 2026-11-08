using Microsoft.AspNetCore.Authentication.JwtBearer;
using SubSnapMS.Identity.Infrastructure.Security.Keys;

namespace SubSnapMS.Identity.API.Authentication;

public static class IdentityJwtValidationExtensions
{
    //BuildingBlocks.Web configura la validazione JWT (issuer, audience, RS256...).
    //In Identity "Auth:MetadataAddress" è vuoto: invece di scaricare il JWKS da se stesso, la validazione usa direttamente le chiavi del key store (comprese quelle in rotazione).
    public static IServiceCollection AddIdentityJwtValidation(this IServiceCollection services)
    {
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .PostConfigure<ISigningKeyStore>((options, keyStore) =>
            {
                options.TokenValidationParameters.IssuerSigningKeyResolver =
                    (_, _, _, _) => keyStore.GetValidationKeys();
            });

        return services;
    }
}