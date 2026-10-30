using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.HealthChecks;

internal static class HealthCheckExtensions
{
    internal const string LivePath = "/health/live";
    internal const string ReadyPath = "/health/ready";

    internal static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)
    {
        // Controllo minimo: se il processo risponde, è vivo
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [HealthCheckTags.Live]);

        return services;
    }

    internal static WebApplication MapDefaultHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Live),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }).AllowAnonymous();

        // Senza controlli "ready" registrati risponde Healthy (è il caso del gateway)
        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }).AllowAnonymous();

        return app;
    }
}
