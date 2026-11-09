using MongoDB.Driver;
using SubSnapMS.Notification.API.Persistence.Documents;

namespace SubSnapMS.Notification.API.Persistence;

//Collezioni di notification_db. IMongoCollection è thread-safe: singleton
public sealed class NotificationDb(IMongoDatabase database)
{
    public IMongoDatabase Database { get; } = database;

    //Copia delle email degli utenti (da Identity)
    public IMongoCollection<RecipientDocument> Recipients { get; } = database.GetCollection<RecipientDocument>("recipients");

    //Copia degli abbonamenti con il prossimo rinnovo (da Subscriptions)
    public IMongoCollection<SubscriptionDocument> Subscriptions { get; } = database.GetCollection<SubscriptionDocument>("subscriptions");

    //Registro invii: una riga per notifica, la chiave impedisce i doppioni.</summary>
    public IMongoCollection<NotificationLogDocument> Notifications { get; } = database.GetCollection<NotificationLogDocument>("notifications");
}