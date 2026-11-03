using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.BuildingBlocks.Observability.Logging;
using SubSnapMS.BuildingBlocks.Observability.OpenTelemetry;
using SubSnapMS.BuildingBlocks.Observability.Sentry;
using SubSnapMS.BuildingBlocks.Observability.Correlation;

namespace SubSnapMS.BuildingBlocks.Observability;

public static class ObservabilityExtensions
{
    //Configura log strutturati, OpenTelemetry (trace, metriche, log), Sentry e health check di base.
    //Da chiamare subito dopo WebApplication.CreateBuilder.

    public static WebApplicationBuilder AddObservability(  //extension method
        this WebApplicationBuilder builder,
        string serviceName,
        Action<ObservabilityOptions>? configure = null)    //è un DELEGATE, cioè è una funzione che il servizio può passare per modificare le opzioni. è sia facoltativo (=null) e nullable (?)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);  //se serviceName è null/vuoto/con spaces allora subito ArgumentException

        // Ordine di precedenza: default → configurazione → delegate. Il nome del servizio lo decide sempre il codice.
        var options = new ObservabilityOptions();  //valori di default della class 
        builder.Configuration.GetSection(ObservabilityOptions.SectionName).Bind(options);  //overwrite con values of appsettings*.json e vars Observability__*
        configure?.Invoke(options);  //esegue il DELEGATE del service (? evita eccezzione se configure=null)
        options.ServiceName = serviceName;  //il nome del servizio lo decide sempre il codice, non la configurazione. override di eventuale valore in appsettings / variabili d'ambiente.
        builder.Services.AddSingleton(options);  
        builder.AddStructuredLogging();  //TraceId/SpanId nei log; console JSON fuori da Development
        builder.AddOpenTelemetryPipeline(options);  //trace, metriche e log OpenTelemetry; export OTLP se c’è l’endpoint
        builder.AddSentryIfConfigured(options);  //invio degli errori a Sentry, solo se c’è un DSN
        builder.Services.AddDefaultHealthChecks();  //il check self (tag live)
        return builder;
    }

    //aggiunge l'header X-Correlation-Id e mappa /health/live e /health/ready.
    //da chiamare dopo UseForwardedHeaders e prima di autenticazione/routing in program.cs
    public static WebApplication UseObservability(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.MapDefaultHealthChecks();

        return app;
    }


}
