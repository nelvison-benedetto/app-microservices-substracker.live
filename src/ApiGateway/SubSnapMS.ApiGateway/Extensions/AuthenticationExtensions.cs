using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using SubSnapMS.ApiGateway.Configuration;

namespace SubSnapMS.ApiGateway.Extensions;

public static class AuthenticationExtensions
{
    // Segnala che il token non è stato verificato perché le chiavi di Identity non erano scaricabili
    private const string SigningKeysUnavailableKey = "gateway:signing-keys-unavailable";

    /// <summary>
    /// Validazione JWT RS256 con le chiavi pubbliche di Identity (scaricate e messe in cache;
    /// se arriva un token firmato con una chiave nuova, JwtBearer riscarica le chiavi da solo).
    /// Authorization: ogni route richiede un utente autenticato, tranne quelle con "AuthorizationPolicy": "anonymous".
    /// </summary>
    public static IServiceCollection AddGatewayAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection(GatewayAuthOptions.SectionName).Get<GatewayAuthOptions>()
                   ?? throw new InvalidOperationException($"Sezione '{GatewayAuthOptions.SectionName}' mancante.");
        auth.Validate();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MetadataAddress = auth.MetadataAddress;
                options.RequireHttpsMetadata = false;         // Identity è raggiunto sulla rete Docker interna
                options.MapInboundClaims = false;             // "sub" resta "sub" (niente nomi lunghi Microsoft)
                options.RefreshOnIssuerKeyNotFound = true;    // rotazione chiavi: riscarica il JWKS se il kid è sconosciuto
                options.SaveToken = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = auth.Issuer,
                    ValidateAudience = true,
                    ValidAudience = auth.Audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    ValidateIssuerSigningKey = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],   // rifiuta "none", HS256 e altri algoritmi
                    ClockSkew = TimeSpan.FromSeconds(auth.ClockSkewSeconds),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Token scaduto/manomesso → SecurityTokenException → 401 normale.
                        // Qualsiasi altra eccezione = chiavi non scaricabili (Identity giù):
                        // non va trattata come "token non valido" ma come servizio non disponibile.
                        if (context.Exception is not SecurityTokenException)
                        {
                            context.HttpContext.Items[SigningKeysUnavailableKey] = true;
                            context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("SubSnapMS.ApiGateway.Authentication")
                                .LogError(context.Exception, "Cannot validate JWT: signing keys unavailable from {MetadataAddress}", auth.MetadataAddress);

                            context.NoResult();   // evita che l'eccezione diventi un 500
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();   // sostituisce la risposta 401 vuota di default

                        if (context.HttpContext.Items.ContainsKey(SigningKeysUnavailableKey))
                        {
                            await GatewayErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status503ServiceUnavailable,
                                "Authentication service temporarily unavailable.");
                            return;
                        }

                        context.Response.Headers.WWWAuthenticate = context.AuthenticateFailure is SecurityTokenExpiredException
                            ? "Bearer error=\"invalid_token\", error_description=\"The token expired\""
                            : "Bearer";

                        await GatewayErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status401Unauthorized);
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Sicuro per default: una route dimenticata senza policy richiede comunque il login
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        return services;
    }
}