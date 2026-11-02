using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.Correlation;


// è un middleware, sostituisce il vecchio CorrelationIdMiddleware del SubSnap monolith.
// Il correlation id ora è il TraceId W3C dato da OpenTelemetry, si propaga da solo tra servizi
// (header traceparent su HTTP, gRPC e RabbitMQ) ed è già presente in ogni log.
// Qui lo rimandiamo al client, così chi segnala un errore può fornirlo.
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ObservabilityOptions options)
{
    public Task InvokeAsync(HttpContext context)  //called ad ogni req, HtttpContext contiene tutto req+response+utente+servizi 
    {
        var correlationId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
        //Activity.Current è l'oggetto OpenTelemetry che contiene il TraceId, se non c'è (non dovrebbe mai succedere) si usa il TraceIdentifier di ASP.NET Core

        context.Response.Headers[options.CorrelationHeaderName] = correlationId;  //scrive negll'header 

        return next(context);
    }
}


