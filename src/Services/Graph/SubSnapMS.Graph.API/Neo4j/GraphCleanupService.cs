using System.Globalization;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using SubSnapMS.Graph.API.Configuration;

namespace SubSnapMS.Graph.API.Neo4j;

/// <summary>
/// Pulizia periodica: tombstone vecchi, servizi senza abbonamenti, utenti senza abbonamenti.
/// Idempotente: con più repliche può girare su tutte.
/// </summary>
internal sealed class GraphCleanupService(
    GraphClient graph,
    GraphSchemaState state,
    IOptions<GraphOptions> options,
    TimeProvider timeProvider,
    ILogger<GraphCleanupService> logger) : BackgroundService
{
    private const string DeleteOldSubscriptionTombstones = """
        MATCH (s:Subscription)
        WHERE s.deleted = true AND s.deletedAt < datetime($cutoff)
        DETACH DELETE s
        RETURN count(s) AS removed
        """;

    private const string DeleteOldDeletedUsers = """
        MATCH (d:DeletedUser)
        WHERE d.deletedAt < datetime($cutoff)
        DELETE d
        RETURN count(d) AS removed
        """;

    private const string DeleteOrphanServices = """
        MATCH (svc:Service)
        WHERE NOT ()-[:OF]->(svc)
        DELETE svc
        RETURN count(svc) AS removed
        """;

    private const string DeleteOrphanUsers = """
        MATCH (u:User)
        WHERE NOT (u)-[:HAS]->()
        DELETE u
        RETURN count(u) AS removed
        """;

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
                    logger.LogError(exception, "Graph cleanup failed");
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
        var cutoff = (timeProvider.GetUtcNow() - options.Value.DeletedTombstoneRetention).UtcDateTime
            .ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

        var tombstones = await RemovedAsync("cleanup-tombstones", DeleteOldSubscriptionTombstones, new { cutoff }, cancellationToken);
        var deletedUsers = await RemovedAsync("cleanup-deleted-users", DeleteOldDeletedUsers, new { cutoff }, cancellationToken);
        var services = await RemovedAsync("cleanup-orphan-services", DeleteOrphanServices, new { }, cancellationToken);
        var users = await RemovedAsync("cleanup-orphan-users", DeleteOrphanUsers, new { }, cancellationToken);

        if (tombstones + deletedUsers + services + users > 0)
        {
            logger.LogInformation(
                "Graph cleanup: {Tombstones} tombstones, {DeletedUsers} deleted users, {Services} services, {Users} users removed",
                tombstones, deletedUsers, services, users);
        }
    }

    private async Task<long> RemovedAsync(string operation, string cypher, object parameters, CancellationToken cancellationToken)
    {
        var records = await graph.WriteAsync(operation, cypher, parameters, cancellationToken);
        return records.Count == 0 ? 0 : records[0]["removed"].As<long>();
    }
}