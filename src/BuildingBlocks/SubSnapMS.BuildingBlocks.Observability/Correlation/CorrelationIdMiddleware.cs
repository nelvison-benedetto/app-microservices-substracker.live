using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Observability.Correlation;

/// <summary>
/// Sostituisce il vecchio CorrelationIdMiddleware del monolite.
/// Il correlation id ora È il TraceId W3C di OpenTelemetry: si propaga da solo tra servizi
/// (header traceparent su HTTP, gRPC e RabbitMQ) ed è già presente in ogni log.
/// Qui lo rimandiamo al client, così chi segnala un errore può fornirlo.
/// </summary>
internal sealed class CorrelationIdMiddleware(RequestDelegate next, ObservabilityOptions options)
{
    public Task InvokeAsync(HttpContext context)
    {
        var correlationId = Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;

        context.Response.Headers[options.CorrelationHeaderName] = correlationId;

        return next(context);
    }
}