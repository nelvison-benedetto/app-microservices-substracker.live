using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SubSnapMS.BuildingBlocks.Web.Errors;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;

namespace SubSnapMS.BuildingBlocks.Web.Authentication;

public static class AuthenticationExtensions
{
    // Segnala che il token non è stato verificato perché le chiavi di Identity non erano scaricabili
    private const string SigningKeysUnavailableKey = "web:signing-keys-unavailable";

    /// <summary>
    /// Validazione JWT RS256 (stesse regole del gateway: difesa in profondità) + authorization sicura per default
    /// + ICurrentUser. Identity aggiunge le proprie chiavi con un PostConfigure su JwtBearerOptions.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var auth = configuration.GetSection(JwtAuthenticationOptions.SectionName).Get<JwtAuthenticationOptions>()
                   ?? throw new InvalidOperationException($"Sezione '{JwtAuthenticationOptions.SectionName}' mancante.");
        auth.Validate();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                if (!string.IsNullOrWhiteSpace(auth.MetadataAddress))
                    options.MetadataAddress = auth.MetadataAddress;

                options.RequireHttpsMetadata = false;         // Identity è raggiunto sulla rete Docker interna
                options.MapInboundClaims = false;             // "sub" resta "sub"
                options.RefreshOnIssuerKeyNotFound = true;    // rotazione delle chiavi
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
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromSeconds(auth.ClockSkewSeconds),
                    NameClaimType = "sub",
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        // Token non valido → SecurityTokenException → 401 normale.
                        // Altro (chiavi non scaricabili, Identity giù) → 503, non "token non valido".
                        if (context.Exception is not SecurityTokenException)
                        {
                            context.HttpContext.Items[SigningKeysUnavailableKey] = true;
                            context.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("SubSnapMS.BuildingBlocks.Web.Authentication")
                                .LogError(context.Exception, "Cannot validate JWT: signing keys unavailable");

                            context.NoResult();
                        }

                        return Task.CompletedTask;
                    },

                    OnChallenge = async context =>
                    {
                        context.HandleResponse();

                        if (context.HttpContext.Items.ContainsKey(SigningKeysUnavailableKey))
                        {
                            await ApiErrorWriter.WriteAsync(context.HttpContext, new ApiError(
                                StatusCodes.Status503ServiceUnavailable,
                                "Authentication service temporarily unavailable.",
                                "auth.unavailable"));
                            return;
                        }

                        var expired = context.AuthenticateFailure is SecurityTokenExpiredException;

                        context.Response.Headers.WWWAuthenticate = expired
                            ? "Bearer error=\"invalid_token\", error_description=\"The token expired\""
                            : "Bearer";

                        await ApiErrorWriter.WriteAsync(context.HttpContext, new ApiError(
                            StatusCodes.Status401Unauthorized,
                            expired ? "The access token expired." : "Authentication required.",
                            expired ? "auth.token_expired" : "unauthorized"));
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Sicuro per default: un endpoint dimenticato senza attributi richiede comunque il login.
            // Gli endpoint pubblici dichiarano esplicitamente .AllowAnonymous().
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }
}