using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.OpenTelemetry;

internal static class OpenTelemetryExtensions
{
    // Variabile d'ambiente standard di OpenTelemetry (es. http://localhost:4317 o http://otel-collector:4317)
    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    internal static WebApplicationBuilder AddOpenTelemetryPipeline(this WebApplicationBuilder builder, ObservabilityOptions options)
    {
        var sourcesPattern = ObservabilityOptions.SourcePrefix + ".*";

        var otel = builder.Services.AddOpenTelemetry()

            // "Chi sono": attributi allegati a ogni trace, metrica e log
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: options.ServiceName,
                    serviceNamespace: "subsnapms",
                    serviceVersion: options.ServiceVersion,
                    serviceInstanceId: Environment.MachineName)   // in Docker = id del container
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment.name"] = builder.Environment.EnvironmentName
                }))

            // TRACE: il percorso di una richiesta attraverso i servizi
            .WithTracing(tracing => tracing
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TraceSamplingRatio)))
                .AddSource(sourcesPattern)              // ActivitySource della piattaforma (messaging, ecc.)
                .AddSource("Yarp.ReverseProxy")         // span interni di YARP, se emessi
                .AddAspNetCoreInstrumentation(aspnet =>
                {
                    aspnet.Filter = context => !IsInfrastructureEndpoint(context.Request.Path);
                })
                .AddHttpClientInstrumentation())        // chiamate HTTP in uscita (anche l'inoltro di YARP)

            // METRICHE: numeri aggregati (richieste/s, latenza, errori, GC, thread pool)
            .WithMetrics(metrics => metrics
                .AddMeter(sourcesPattern)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())

            // LOG: l'ILogger di .NET esportato via OTLP, con TraceId/SpanId allegati
            .WithLogging(
                configureBuilder: null,
                configureOptions: logging =>
                {
                    logging.IncludeFormattedMessage = true;
                    logging.IncludeScopes = true;
                    logging.ParseStateValues = true;
                });

        // Export solo se c'è un endpoint: senza, il servizio funziona uguale e non esporta nulla
        if (!string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]))
        {
            otel.UseOtlpExporter();
        }

        return builder;
    }

    // Docker interroga gli health check ogni pochi secondi: senza filtro riempirebbero le trace
    private static bool IsInfrastructureEndpoint(PathString path) =>
        path.StartsWithSegments("/health");
}