using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.Sentry;

internal static class SentryExtensions
{
    internal static WebApplicationBuilder AddSentryIfConfigured(this WebApplicationBuilder builder, ObservabilityOptions options)
    {
        // DSN vuoto = Sentry spento (in locale)
        if (string.IsNullOrWhiteSpace(builder.Configuration["Sentry:Dsn"]))
            return builder;

        // Le altre opzioni Sentry (es. MinimumEventLevel) si impostano nella sezione "Sentry" della configurazione
        builder.WebHost.UseSentry(sentry =>
        {
            sentry.Environment = builder.Environment.EnvironmentName;
            sentry.Release = $"{options.ServiceName}@{options.ServiceVersion}";
            sentry.SendDefaultPii = false;

            // Le trace le gestisce OpenTelemetry: Sentry serve solo per gli errori
            sentry.TracesSampleRate = 0;

            // Collega ogni errore su Sentry alla trace OpenTelemetry corrispondente
            sentry.SetBeforeSend((sentryEvent, _) =>
            {
                sentryEvent.SetTag("service", options.ServiceName);

                var activity = Activity.Current;
                if (activity is not null)
                    sentryEvent.SetTag("trace_id", activity.TraceId.ToHexString());

                return sentryEvent;
            });
        });

        return builder;
    }
}