using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Observability.HealthChecks;

// Risposta JSON leggibile al posto del semplice "Healthy".
// Non include messaggi di eccezione, un health check non deve rivelare dettagli interni.
// Status code (deciso da ASP.NET Core): 200 se Healthy/Degraded, 503 se Unhealthy.
internal static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);  //applichi impostazioni di aspnetcore utilizzate da aspnetcore per le api, cioè camelCase e case-insensitive cosi è uniforme con aspnetcore

    internal static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";  //dice al client che il body è JSON in UTF-8
        context.Response.Headers.CacheControl = "no-store";  //vieta a qualsiasi cache (browser, proxy, CDN) di conservare la risposta

        var payload = new  //obj senza classe dichiarata
        {
            status = report.Status.ToString(),  //enum HealthStatus diventa "Healthy"/"Degraded" o "Unhealthy" invece di 2, 1, 0
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),  //timespan serializzato in milliseconds con 2 decimali, piu facile da leggere e confrontare
            checks = report.Entries.Select(entry => new  //.Select() è LINQ, per ogni health check (entry) crea un oggetto anonimo con i campi name, status, durationMs e description
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                description = entry.Value.Description
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));  //serializza il payload in stringa JSON e la mette nel body della risposta HTTP, con le impostazioni di serializzazione definite sopra
    }
}

