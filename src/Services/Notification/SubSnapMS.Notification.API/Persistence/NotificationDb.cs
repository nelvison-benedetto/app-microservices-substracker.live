using MongoDB.Driver;
using SubSnapMS.Notification.API.Persistence.Documents;

namespace SubSnapMS.Notification.API.Persistence;

/// <summary>Collezioni di notification_db. IMongoCollection è thread-safe: singleton.</summary>
public sealed class NotificationDb(IMongoDatabase database)
{
    public IMongoDatabase Database { get; } = database;

    /// <summary>Copia delle email degli utenti (da Identity).</summary>
    public IMongoCollection<RecipientDocument> Recipients { get; } = database.GetCollection<RecipientDocument>("recipients");

    /// <summary>Copia degli abbonamenti con il prossimo rinnovo (da Subscriptions).</summary>
    public IMongoCollection<SubscriptionDocument> Subscriptions { get; } = database.GetCollection<SubscriptionDocument>("subscriptions");

    /// <summary>Registro invii: una riga per notifica, la chiave impedisce i doppioni.</summary>
    public IMongoCollection<NotificationLogDocument> Notifications { get; } = database.GetCollection<NotificationLogDocument>("notifications");
}