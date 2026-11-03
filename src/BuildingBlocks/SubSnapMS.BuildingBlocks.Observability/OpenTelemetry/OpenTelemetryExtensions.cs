using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;


namespace SubSnapMS.BuildingBlocks.Observability.OpenTelemetry;

//configura OpenTelemetry libs per tracce, metriche e logs
internal static class OpenTelemetryExtensions
{
    // Variabile d'ambiente standard di OpenTelemetry (es. http://localhost:4317 o http://otel-collector:4317)
    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";  //nome standart della var, la trovi in launchsettings.json e in docker-compose.override.yml

    internal static WebApplicationBuilder AddOpenTelemetryPipeline(this WebApplicationBuilder builder, ObservabilityOptions options)   
    {

        var platformPattern = ObservabilityOptions.SourcePrefix + ".*";  //"SubSnapMS.*" xk tutte le sorgenti iniziano con 'SubSnapMS.'(e.g.SubSnapMS.Notification)

        var otel = builder.Services.AddOpenTelemetry()  //registra l'sdk nel DI, e il builder che restituisce (cioe 'otel') lo configuri dopo 

            
            .ConfigureResource(resource => resource  //"Chi sono": attributi allegati a ogni trace, metrica e log
                .AddService(
                    serviceName: options.ServiceName,
                    serviceNamespace: "subsnapms",
                    serviceVersion: options.ServiceVersion,   //version dell'assembly, es. 1.0.0
                    serviceInstanceId: Environment.MachineName)   // in Docker, è l'hostname del container (cioè il suo id)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment.name"] = builder.Environment.EnvironmentName
                }))

            //TRACE: il percorso di una richiesta attraverso i servizi
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TraceSamplingRatio)))  //e.g. TraceIdRatioBasedSampler(0.25) tiene circa il 25% delle trace, con ParentBasedSampler il ratio si applica solo agli span radice , altrimenti eredita quello del padre
                    .AddSource(platformPattern)    //"SubSnapMS.*"
                    .AddSource("Yarp.ReverseProxy")  //span interni di YARP (solo nel gateway)
                    .AddAspNetCoreInstrumentation(aspnet =>
                    {
                        aspnet.Filter = context => !IsInfrastructureEndpoint(context.Request.Path);
                        aspnet.RecordException = true;  //se la richiesta lancia un’eccezione, tipo, messaggio e stack trace vengono allegati allo span come evento.
                    })  //uno span per ogni richiesta HTTP in ingresso, con TraceId/SpanId allegati al log
                    .AddHttpClientInstrumentation(http => http.RecordException = true);   //uno span per ogni chiamata HTTP in uscita, e aggiunge anche header 'traceparent' alle req in uscita.

                foreach (var source in options.AdditionalSources.Where(s => !string.IsNullOrWhiteSpace(s)))
                    tracing.AddSource(source);  //aggiunge le sorgenti dei singoli servizi, e.g. 'Npgsql' cosi .Observability non deve conoscere le libs di tutti i services.
            })

            // METRICHE: numeri aggregati (richieste/s, latenza, errori, GC, thread pool)
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(platformPattern)  //mine (es. subsnapms.notification.emails, errori dell'Outbox)
                    .AddAspNetCoreInstrumentation()  //richieste/s, durata, status code, connessioni attive
                    .AddHttpClientInstrumentation()  //durata e errori delle chiamate in uscita 
                    .AddRuntimeInstrumentation();  //GC, memoria, thread pool, eccezioni

                foreach (var meter in options.AdditionalMeters.Where(m => !string.IsNullOrWhiteSpace(m)))
                    metrics.AddMeter(meter);
            })

            // LOG: Aggiunge un secondo provider a ILogger, accanto alla console: ogni logger.LogXxx(...) esce anche via OTLP con TraceId/SpanId allegati (nella dashboard, dalla trace clicchi e vedi i logs di quella req, e viceversa🔥)
            .WithLogging(
                configureBuilder: null,  //non serve personalizzare il builder dei log, intanto lo esporterò solo successivamente 
                configureOptions: logging =>
                {
                    logging.IncludeFormattedMessage = true;  //oltre al template con interpolation e.g.("Subscription {Id} created") viene aggiunto anche il testo finale e.g.("Subscription 0193… created") leggibile
                    logging.IncludeScopes = true;  //include gli scope, es. RequestPath e ConnectionId aggiunti da ASP.NET Core
                    logging.ParseStateValues = true;  //i parametri del messaggio diventano attributi separati (Id = 0193…), nel backend puoi cercare “tutti i log con Id = 0193…” invece di fare ricerche testuali
                });

        // Export solo se c'è un endpoint: senza, il servizio funziona uguale e non esporta nulla
        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
        {
            otel.UseOtlpExporter();  //aggiunge l'esportatore OTLP (OpenTelemetry Protocol) che invia trace, metriche e log al collector o backend configurato nell'endpoint OTEL_EXPORTER_OTLP_ENDPOINT. legge da solo le vars OTEL_EXPORTER_OTLP_ENDPOINT, OTEL_EXPORTER_OTLP_PROTOCOL (default grpc, porta 4317; http/protobuf usa la 4318) e OTEL_EXPORTER_OTLP_HEADERS (es. una API key per un servizio cloud)
        }

        return builder;
    }

    // Docker e YARP interrogano gli health check ogni pochi secondi: senza filtro riempirebbero le trace
    private static bool IsInfrastructureEndpoint(PathString path) =>
        path.StartsWithSegments("/health");  //confronta solo il path, senza query string, es. /health/live o /health/ready
}