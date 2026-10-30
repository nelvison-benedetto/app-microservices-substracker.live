using SubSnapMS.ApiGateway.Configuration;
using SubSnapMS.ApiGateway.Errors;
using System.Globalization;
using System.Threading.RateLimiting;

namespace SubSnapMS.ApiGateway.Extensions;


public static class RateLimitingExtensions
{
    /// <summary>Nome della policy da usare nelle route YARP: "RateLimiterPolicy": "auth".</summary>
    public const string AuthPolicy = "auth";

    private const string Unlimited = "unlimited";

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(GatewayRateLimitingOptions.SectionName).Get<GatewayRateLimitingOptions>()
                      ?? new GatewayRateLimitingOptions();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                return new ValueTask(GatewayErrorWriter.WriteAsync(context.HttpContext, StatusCodes.Status429TooManyRequests));
            };

            // Globale: per utente se autenticato (il JWT è già stato validato), altrimenti per IP. Health check esclusi.
            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (!options.Enabled || httpContext.Request.Path.StartsWithSegments("/health"))
                    return RateLimitPartition.GetNoLimiter(Unlimited);

                return RateLimitPartition.GetFixedWindowLimiter(UserOrIpKey(httpContext), _ => ToLimiterOptions(options.Global));
            });

            // "auth": sempre per IP (su login e registrazione l'utente non è ancora autenticato)
            limiter.AddPolicy(AuthPolicy, httpContext =>
                options.Enabled
                    ? RateLimitPartition.GetFixedWindowLimiter(IpKey(httpContext), _ => ToLimiterOptions(options.Auth))
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

    private static string UserOrIpKey(HttpContext httpContext)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value;
        return userId is not null ? $"user:{userId}" : IpKey(httpContext);
    }

    // Dopo UseForwardedHeaders, RemoteIpAddress è l'IP reale del client
    private static string IpKey(HttpContext httpContext) =>
        $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
}


