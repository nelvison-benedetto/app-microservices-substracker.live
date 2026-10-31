using MongoDB.Driver;
using SubSnapMS.Analytics.API.Persistence.Documents;

namespace SubSnapMS.Analytics.API.Persistence;

/// <summary>Collezioni di analytics_db. IMongoCollection è thread-safe: singleton.</summary>
public sealed class AnalyticsDb(IMongoDatabase database)
{
    public IMongoDatabase Database { get; } = database;

    /// <summary>Copia degli abbonamenti (da Subscriptions), inclusi i tombstone.</summary>
    public IMongoCollection<SubscriptionDocument> Subscriptions { get; } = database.GetCollection<SubscriptionDocument>("subscriptions");

    /// <summary>Utenti cancellati: bloccano gli eventi arrivati in ritardo.</summary>
    public IMongoCollection<DeletedUserDocument> DeletedUsers { get; } = database.GetCollection<DeletedUserDocument>("deleted_users");
}