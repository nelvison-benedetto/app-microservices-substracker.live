using SubSnapMS.ApiGateway.Errors;
using System.Globalization;
using System.Threading.RateLimiting;

namespace SubSnapMS.ApiGateway.Extensions;


public sealed class GatewayRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Limite generale per IP, su tutte le richieste.</summary>
    public FixedWindowSettings Global { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60 };

    /// <summary>Limite stretto per IP su login e registrazione (anti brute force).</summary>
    public FixedWindowSettings Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}

public sealed class FixedWindowSettings
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}

public static class RateLimitingExtensions
{
    /// <summary>Nome della policy usata nelle route YARP ("RateLimiterPolicy": "auth").</summary>
    public const string AuthPolicy = "auth";

    private const string Unlimited = "unlimited";

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(GatewayRateLimitingOptions.SectionName).Get<GatewayRateLimitingOptions>()
                      ?? new GatewayRateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                await context.HttpContext.Response.WriteAsJsonAsync(
                    GatewayErrorResponse.Create(StatusCodes.Status429TooManyRequests, "Too many requests. Please try again later."),
                    cancellationToken);
            };

            // Limite globale: tutte le richieste, partizionate per IP (health check esclusi)
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!options.Enabled || httpContext.Request.Path.StartsWithSegments("/health"))
                    return RateLimitPartition.GetNoLimiter(Unlimited);

                return RateLimitPartition.GetFixedWindowLimiter(ClientKey(httpContext), _ => ToLimiterOptions(options.Global));
            });

            // Policy "auth": applicata solo alle route YARP che la dichiarano
            limiter.AddPolicy(AuthPolicy, httpContext =>
                options.Enabled
                    ? RateLimitPartition.GetFixedWindowLimiter(ClientKey(httpContext), _ => ToLimiterOptions(options.Auth))
                    : RateLimitPartition.GetNoLimiter(Unlimited));
        });

        return services;
    }

    private static FixedWindowRateLimiterOptions ToLimiterOptions(FixedWindowSettings settings) => new()
    {
        PermitLimit = settings.PermitLimit,
        Window = TimeSpan.FromSeconds(settings.WindowSeconds),
        QueueLimit = 0,              // oltre il limite si risponde subito 429, niente coda
        AutoReplenishment = true
    };

    // Dopo UseForwardedHeaders, RemoteIpAddress è l'IP reale del client
    private static string ClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}


