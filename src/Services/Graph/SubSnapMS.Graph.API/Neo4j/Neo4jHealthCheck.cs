using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SubSnapMS.Graph.API.Neo4j;

/// <summary>Ready = Neo4j raggiungibile e schema applicato.</summary>
internal sealed class Neo4jHealthCheck(GraphClient graph, GraphSchemaState state) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await graph.VerifyConnectivityAsync();

            return state.IsReady
                ? HealthCheckResult.Healthy()
                : new HealthCheckResult(context.Registration.FailureStatus, "Neo4j schema is not initialized yet.");
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Neo4j is unreachable.", exception);
        }
    }
}