namespace SubSnapMS.ApiGateway.Extensions;

public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Header di sicurezza su ogni risposta (se il servizio non li ha già impostati).
    /// HSTS e compressione sono compito di Traefik, che termina il TLS.
    /// </summary>
    public static WebApplication UseSecurityHeaders(this WebApplication app)
    {
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("X-Frame-Options", "DENY");
                headers.TryAdd("Referrer-Policy", "no-referrer");
                return Task.CompletedTask;
            });

            return next(context);
        });

        return app;
    }
}
