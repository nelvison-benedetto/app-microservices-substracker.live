using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Web.Authentication;
using SubSnapMS.Identity.Infrastructure.Security.Keys;

namespace SubSnapMS.Identity.API.Endpoints.WellKnown;

/// <summary>
/// Discovery e chiavi pubbliche, nel formato standard OpenID Connect / JWK.
/// Li leggono gateway e servizi (JwtBearer con MetadataAddress) sulla rete interna:
/// il gateway NON li espone (nessuna route /.well-known).
/// Fuori dal gruppo /api/v1: i percorsi /.well-known sono fissati dagli standard.
/// </summary>
internal static class WellKnownEndpoints
{
    private const string CacheControl = "public, max-age=300";

    public static IEndpointRouteBuilder MapWellKnownEndpoints(this IEndpointRouteBuilder routes)
    {
        var wellKnown = routes.MapGroup("/.well-known")
            .AllowAnonymous()
            .ExcludeFromDescription();

        wellKnown.MapGet("openid-configuration", (HttpContext context, IConfiguration configuration) =>
        {
            var auth = configuration.GetSection(JwtAuthenticationOptions.SectionName).Get<JwtAuthenticationOptions>()!;

            // jwks_uri costruito dall'host della richiesta: chi chiama sulla rete interna riceve un indirizzo interno
            var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

            context.Response.Headers.CacheControl = CacheControl;
            return Results.Json(new
            {
                issuer = auth.Issuer,
                jwks_uri = $"{baseUrl}/.well-known/jwks.json",
                id_token_signing_alg_values_supported = new[] { "RS256" }
            });
        });

        wellKnown.MapGet("jwks.json", (HttpContext context, ISigningKeyStore keyStore) =>
        {
            context.Response.Headers.CacheControl = CacheControl;
            return Results.Json(new
            {
                keys = keyStore.GetPublicKeys().Select(key => new
                {
                    kty = "RSA",
                    use = "sig",
                    alg = "RS256",
                    kid = key.KeyId,
                    n = key.Modulus,     // base64url
                    e = key.Exponent     // base64url
                })
            });
        });

        return routes;
    }
}