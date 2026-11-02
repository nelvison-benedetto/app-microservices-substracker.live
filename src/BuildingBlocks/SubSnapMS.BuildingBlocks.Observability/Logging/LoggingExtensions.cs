using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SubSnapMS.BuildingBlocks.Observability.Logging;

internal static class LoggingExtensions
{
    internal static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)  //method extension, viene aggiunto a 'WebApplicationBuilder'
    {
        //ogni log scritto durante una richiesta porta con sé TraceId e SpanId,
        //anche sulla console (utile con "docker logs").
        builder.Logging.Configure(options =>  //modifica le LoggerFactoryOptions, cioè le impostazioni globali della fabbrica dei logger
            options.ActivityTrackingOptions = //per ogni log, il logger guarda Activity.Current (la stessa del CorrelationIdMiddleware) e aggiunge i campi scelti come scope,
                ActivityTrackingOptions.TraceId |  //TraceId è il correlation id, lo stesso che OpenTelemetry mette in ogni log e che il CorrelationIdMiddleware rimanda al client
                ActivityTrackingOptions.SpanId |  //l’id del singolo pezzo di lavoro in questo servizio (questa richiesta, questa chiamata)
                ActivityTrackingOptions.ParentId);  //l’id del pezzo di lavoro che ha chiamato questo servizio (la richiesta HTTP entrante, la chiamata gRPC, il messaggio RabbitMQ)

        // Fuori dallo sviluppo: console in JSON, una riga per log, leggibile da qualsiasi aggregatore.
        // In sviluppo resta la console colorata di default, più leggibile a occhio.
        if (!builder.Environment.IsDevelopment())  //legge var ASPNETCORE_ENVIRONMENT in launchsettings.json 
        {
            builder.Logging.AddJsonConsole(console =>
            {
                console.IncludeScopes = true;  //include i campi TraceId e SpanId (vedi sopra) in ogni log
                console.UseUtcTimestamp = true;  //orario in UTC
                console.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";  //format dalla date
            });
        }

        return builder;
    }
}