using Microsoft.AspNetCore.Builder;
using SubSnapMS.BuildingBlocks.Observability.Correlation;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Configura log strutturati, OpenTelemetry (trace, metriche, log), Sentry e health check di base.
    /// Da chiamare subito dopo WebApplication.CreateBuilder.
    /// </summary>
    /// <param name="serviceName">Nome del servizio: "api-gateway", "identity", "subscriptions", ...</param>
    /// <param name="configure">Override opzionali via codice (vincono sulla configurazione).</param>
    public static WebApplicationBuilder AddObservability(
        this WebApplicationBuilder builder,
        string serviceName,
        Action<ObservabilityOptions>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        // Ordine di precedenza: default → configurazione → delegate. Il nome del servizio lo decide sempre il codice.
        var options = new ObservabilityOptions();
        builder.Configuration.GetSection(ObservabilityOptions.SectionName).Bind(options);
        configure?.Invoke(options);
        options.ServiceName = serviceName;

        builder.Services.AddSingleton(options);

        builder.AddStructuredLogging();
        builder.AddOpenTelemetryPipeline(options);
        builder.AddSentryIfConfigured(options);
        builder.Services.AddDefaultHealthChecks();

        return builder;
    }

    /// <summary>
    /// Aggiunge l'header X-Correlation-Id e mappa /health/live e /health/ready.
    /// Da chiamare dopo UseForwardedHeaders e prima di autenticazione/routing.
    /// </summary>
    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.MapDefaultHealthChecks();

        return app;
    }
}
