using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SubSnapMS.Notification.API.Configuration;
using SubSnapMS.Notification.API.Persistence.Documents;

namespace SubSnapMS.Notification.API.Persistence;

/// <summary>
/// Crea gli indici all'avvio (idempotente). In background con retry: se Mongo non è ancora pronto
/// il servizio parte lo stesso e /health/ready resta Unhealthy finché Mongo non risponde.
/// Nota: cambiando LogRetention o DeletedTombstoneRetention va eliminato a mano il vecchio indice TTL
/// (Mongo rifiuta un indice con lo stesso nome e opzioni diverse).
/// </summary>
internal sealed class MongoIndexInitializer(
    NotificationDb db,
    IOptions<NotificationOptions> options,
    ILogger<MongoIndexInitializer> logger) : BackgroundService
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
        var settings = options.Value;

        await db.Recipients.Indexes.CreateOneAsync(
            new CreateIndexModel<RecipientDocument>(
                Builders<RecipientDocument>.IndexKeys.Ascending(d => d.ExpireAtUtc),
                new CreateIndexOptions { Name = "ttl_expire_at", ExpireAfter = TimeSpan.Zero }),
            cancellationToken: cancellationToken);

        await db.Subscriptions.Indexes.CreateManyAsync(
        [
            // Promemoria da inviare: ReminderPending + intervallo di date
            new CreateIndexModel<SubscriptionDocument>(
                Builders<SubscriptionDocument>.IndexKeys.Ascending(d => d.ReminderPending).Ascending(d => d.NextRenewalDate),
                new CreateIndexOptions { Name = "ix_reminder_due" }),
            // Date di rinnovo superate da far avanzare
            new CreateIndexModel<SubscriptionDocument>(
                Builders<SubscriptionDocument>.IndexKeys.Ascending(d => d.Deleted).Ascending(d => d.NextRenewalDate),
                new CreateIndexOptions { Name = "ix_deleted_next_renewal" }),
            // Cancellazione account
            new CreateIndexModel<SubscriptionDocument>(
                Builders<SubscriptionDocument>.IndexKeys.Ascending(d => d.UserId),
                new CreateIndexOptions { Name = "ix_user_id" }),
            new CreateIndexModel<SubscriptionDocument>(
                Builders<SubscriptionDocument>.IndexKeys.Ascending(d => d.ExpireAtUtc),
                new CreateIndexOptions { Name = "ttl_expire_at", ExpireAfter = TimeSpan.Zero })
        ], cancellationToken);

        await db.Notifications.Indexes.CreateManyAsync(
        [
            new CreateIndexModel<NotificationLogDocument>(
                Builders<NotificationLogDocument>.IndexKeys.Ascending(d => d.UserId),
                new CreateIndexOptions { Name = "ix_user_id" }),
            new CreateIndexModel<NotificationLogDocument>(
                Builders<NotificationLogDocument>.IndexKeys.Ascending(d => d.CreatedAtUtc),
                new CreateIndexOptions { Name = "ttl_created_at", ExpireAfter = settings.Delivery.LogRetention })
        ], cancellationToken);
    }
}