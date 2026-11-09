using MongoDB.Driver;
using SubSnapMS.Analytics.API.Persistence.Documents;

namespace SubSnapMS.Analytics.API.Persistence;

//Collezioni di analytics_db. IMongoCollection è thread-safe: singleton
public sealed class AnalyticsDb(IMongoDatabase database)
{
    public IMongoDatabase Database { get; } = database;

    //Copia degli abbonamenti (da Subscriptions), inclusi i tombstone
    public IMongoCollection<SubscriptionDocument> Subscriptions { get; } = database.GetCollection<SubscriptionDocument>("subscriptions");

    //Utenti cancellati: bloccano gli eventi arrivati in ritardo
    public IMongoCollection<DeletedUserDocument> DeletedUsers { get; } = database.GetCollection<DeletedUserDocument>("deleted_users");
}