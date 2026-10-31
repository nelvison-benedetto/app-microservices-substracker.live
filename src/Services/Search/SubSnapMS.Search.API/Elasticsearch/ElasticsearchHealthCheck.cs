using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SubSnapMS.Search.API.Elasticsearch;

/// <summary>Ready = cluster raggiungibile, non "red", e indici inizializzati.</summary>
internal sealed class ElasticsearchHealthCheck(ElasticHttpClient elastic, SearchIndexState state) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await elastic.GetClusterStatusAsync(cancellationToken);

            if (status == "red")
                return new HealthCheckResult(context.Registration.FailureStatus, "Elasticsearch cluster status is red.");

            if (!state.IsReady)
                return new HealthCheckResult(context.Registration.FailureStatus, "Elasticsearch indices are not initialized yet.");

            return status == "yellow"
                ? HealthCheckResult.Degraded("Elasticsearch cluster status is yellow.")
                : HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Elasticsearch is unreachable.", exception);
        }
    }
}