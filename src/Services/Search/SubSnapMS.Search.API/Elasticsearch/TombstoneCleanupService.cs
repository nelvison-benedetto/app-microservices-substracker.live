using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using SubSnapMS.Search.API.Configuration;

namespace SubSnapMS.Search.API.Elasticsearch;

/// <summary>
/// Rimuove i tombstone più vecchi della retention (Elasticsearch non ha un TTL come Mongo).
/// Con più repliche gira su tutte: delete_by_query è idempotente.
/// </summary>
internal sealed class TombstoneCleanupService(
    IServiceScopeFactory scopeFactory,
    SearchIndexState state,
    IOptions<SearchOptions> options,
    TimeProvider timeProvider,
    ILogger<TombstoneCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await state.WaitUntilReadyAsync(stoppingToken);
            using var timer = new PeriodicTimer(options.Value.CleanupInterval, timeProvider);

            do
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Tombstone cleanup failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown normale
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var elastic = scope.ServiceProvider.GetRequiredService<ElasticHttpClient>();

        var cutoff = (timeProvider.GetUtcNow() - options.Value.DeletedTombstoneRetention).ToString("O");

        var subscriptions = await elastic.DeleteByQueryAsync(
            SearchIndices.Subscriptions,
            EsQuery.Bool(filter: new JsonArray(
                EsQuery.Term("deleted", true),
                EsQuery.Range("updatedAt", "lt", cutoff))),
            cancellationToken);

        var users = await elastic.DeleteByQueryAsync(
            SearchIndices.DeletedUsers,
            EsQuery.Range("deletedAt", "lt", cutoff),
            cancellationToken);

        if (subscriptions + users > 0)
            logger.LogInformation("Tombstone cleanup: {Subscriptions} subscriptions, {Users} users removed", subscriptions, users);
    }
}