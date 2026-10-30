using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// Health check "ready". In caso di problema restituisce lo stato di fallimento della registrazione
/// (Degraded): il servizio resta nel traffico, l'Outbox trattiene gli eventi, il problema è visibile.
/// </summary>
internal sealed class RabbitMqHealthCheck(RabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var rabbitConnection = await connection.GetConnectionAsync(cancellationToken);

            return rabbitConnection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ connection is open.")
                : new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ connection is recovering.");
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "RabbitMQ is unreachable.", exception);
        }
    }
}