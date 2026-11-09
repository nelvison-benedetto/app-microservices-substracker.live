using Microsoft.Extensions.Options;
using SubSnapMS.Search.API.Configuration;

namespace SubSnapMS.Search.API.Elasticsearch;

//Crea gli indici (con alias) se l'alias non esiste. Idempotente e sicuro con più repliche. In background con retry: finché Elasticsearch non risponde, /health/ready è Unhealthy.
internal sealed class IndexInitializer(
    IServiceScopeFactory scopeFactory,
    SearchIndexState state,
    IOptions<ElasticsearchOptions> options,
    ILogger<IndexInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await EnsureIndicesAsync(stoppingToken);
                    state.MarkReady();
                    logger.LogInformation("Elasticsearch indices ready");
                    return;
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    var delay = TimeSpan.FromSeconds(Math.Min(60, 2 * attempt));
                    logger.LogWarning(exception, "Elasticsearch index initialization failed (attempt {Attempt}). Retrying in {Delay}", attempt, delay);
                    await Task.Delay(delay, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown durante l'attesa
        }
    }

    private async Task EnsureIndicesAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var elastic = scope.ServiceProvider.GetRequiredService<ElasticHttpClient>();
        var replicas = options.Value.NumberOfReplicas;

        await EnsureAsync(elastic, SearchIndices.Subscriptions, SearchIndices.SubscriptionsV1, SearchIndices.SubscriptionsDefinition(replicas), cancellationToken);
        await EnsureAsync(elastic, SearchIndices.DeletedUsers, SearchIndices.DeletedUsersV1, SearchIndices.DeletedUsersDefinition(replicas), cancellationToken);
    }

    private async Task EnsureAsync(ElasticHttpClient elastic, string alias, string index, string definition, CancellationToken cancellationToken)
    {
        if (await elastic.ExistsAsync($"_alias/{alias}", cancellationToken))
            return;

        var created = await elastic.CreateIndexAsync(index, definition, cancellationToken);
        logger.LogInformation(created ? "Index {Index} created (alias {Alias})" : "Index {Index} already exists", index, alias);
    }
}