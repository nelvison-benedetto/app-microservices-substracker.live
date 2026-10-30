using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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
        var platformPattern = ObservabilityOptions.SourcePrefix + ".*";

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
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(options.TraceSamplingRatio)))
                    .AddSource(platformPattern)              // messaging e altre sorgenti della piattaforma
                    .AddSource("Yarp.ReverseProxy")          // span interni di YARP (solo nel gateway)
                    .AddAspNetCoreInstrumentation(aspnet =>
                    {
                        aspnet.Filter = context => !IsInfrastructureEndpoint(context.Request.Path);
                        aspnet.RecordException = true;
                    })
                    .AddHttpClientInstrumentation(http => http.RecordException = true);   // HTTP e gRPC in uscita, inoltro di YARP

                foreach (var source in options.AdditionalSources.Where(s => !string.IsNullOrWhiteSpace(s)))
                    tracing.AddSource(source);
            })

            // METRICHE: numeri aggregati (richieste/s, latenza, errori, GC, thread pool)
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(platformPattern)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                foreach (var meter in options.AdditionalMeters.Where(m => !string.IsNullOrWhiteSpace(m)))
                    metrics.AddMeter(meter);
            })

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

    // Docker e YARP interrogano gli health check ogni pochi secondi: senza filtro riempirebbero le trace
    private static bool IsInfrastructureEndpoint(PathString path) =>
        path.StartsWithSegments("/health");
}