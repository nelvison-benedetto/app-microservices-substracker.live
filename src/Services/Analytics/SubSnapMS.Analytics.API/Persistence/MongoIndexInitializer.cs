using MongoDB.Driver;
using SubSnapMS.Analytics.API.Persistence.Documents;

namespace SubSnapMS.Analytics.API.Persistence;

/// <summary>Crea gli indici all'avvio (idempotente), in background con retry se Mongo non è ancora pronto.</summary>
internal sealed class MongoIndexInitializer(AnalyticsDb db, ILogger<MongoIndexInitializer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await CreateIndexesAsync(stoppingToken);
                    logger.LogInformation("MongoDB indexes ready");
                    return;
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    var delay = TimeSpan.FromSeconds(Math.Min(60, 2 * attempt));
                    logger.LogWarning(exception, "MongoDB index creation failed (attempt {Attempt}). Retrying in {Delay}", attempt, delay);
                    await Task.Delay(delay, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown durante l'attesa
        }
    }

    private async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        await db.Subscriptions.Indexes.CreateManyAsync(
        [
            // La query della dashboard: tutti gli abbonamenti (non cancellati) di un utente
            new CreateIndexModel<SubscriptionDocument>(
                Builders<SubscriptionDocument>.IndexKeys.Ascending(d => d.UserId).Ascending(d => d.Deleted),
                new CreateIndexOptions { Name = "ix_user_id_deleted" }),
            new CreateIndexModel<SubscriptionDocument>(
                Builders<SubscriptionDocument>.IndexKeys.Ascending(d => d.ExpireAtUtc),
                new CreateIndexOptions { Name = "ttl_expire_at", ExpireAfter = TimeSpan.Zero })
        ], cancellationToken);

        await db.DeletedUsers.Indexes.CreateOneAsync(
            new CreateIndexModel<DeletedUserDocument>(
                Builders<DeletedUserDocument>.IndexKeys.Ascending(d => d.ExpireAtUtc),
                new CreateIndexOptions { Name = "ttl_expire_at", ExpireAfter = TimeSpan.Zero }),
            cancellationToken: cancellationToken);
    }
}