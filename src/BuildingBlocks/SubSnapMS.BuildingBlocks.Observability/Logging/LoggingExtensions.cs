using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.Logging;

internal static class LoggingExtensions
{
    internal static WebApplicationBuilder AddStructuredLogging(this WebApplicationBuilder builder)
    {
        // Ogni log scritto durante una richiesta porta con sé TraceId e SpanId,
        // anche sulla console (utile con "docker logs").
        builder.Logging.Configure(options =>
            options.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId |
                ActivityTrackingOptions.SpanId |
                ActivityTrackingOptions.ParentId);

        // Fuori dallo sviluppo: console in JSON, una riga per log, leggibile da qualsiasi aggregatore.
        // In sviluppo resta la console colorata di default, più leggibile a occhio.
        if (!builder.Environment.IsDevelopment())
        {
            builder.Logging.AddJsonConsole(console =>
            {
                console.IncludeScopes = true;
                console.UseUtcTimestamp = true;
                console.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
            });
        }

        return builder;
    }
}