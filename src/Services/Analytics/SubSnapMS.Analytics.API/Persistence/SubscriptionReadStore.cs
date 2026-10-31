using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SubSnapMS.Analytics.API.Calculations;
using SubSnapMS.Analytics.API.Configuration;
using SubSnapMS.Analytics.API.Persistence.Documents;

namespace SubSnapMS.Analytics.API.Persistence;

/// <summary>Stato completo di un abbonamento, preso da SubscriptionCreated/Updated.</summary>
public sealed record SubscriptionSnapshot(
    Guid SubscriptionId,
    Guid UserId,
    long Version,
    string Name,
    string? Category,
    decimal Amount,
    string Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed class SubscriptionReadStore(AnalyticsDb db, IOptions<AnalyticsOptions> options)
{
    private static FilterDefinitionBuilder<SubscriptionDocument> Filter => Builders<SubscriptionDocument>.Filter;
    private static UpdateDefinitionBuilder<SubscriptionDocument> Update => Builders<SubscriptionDocument>.Update;

    /// <summary>
    /// Applica lo stato solo se la Version è più nuova. Se il documento ha una Version uguale o maggiore
    /// (o è un tombstone) il filtro non lo trova, l'upsert prova a inserirlo e l'_id lo rifiuta: evento ignorato.
    /// </summary>
    public async Task UpsertAsync(SubscriptionSnapshot snapshot, CancellationToken cancellationToken)
    {
        var filter = Filter.Eq(d => d.SubscriptionId, snapshot.SubscriptionId)
                     & Filter.Lt(d => d.Version, snapshot.Version);

        var update = Update
            .Set(d => d.UserId, snapshot.UserId)
            .Set(d => d.Version, snapshot.Version)
            .Set(d => d.Deleted, false)
            .Set(d => d.Name, snapshot.Name)
            .Set(d => d.Category, snapshot.Category)
            .Set(d => d.Amount, snapshot.Amount)
            .Set(d => d.Currency, snapshot.Currency)
            .Set(d => d.BillingCycle, snapshot.BillingCycle)
            .Set(d => d.StartDate, snapshot.StartDate.ToBsonDate())
            .Set(d => d.EndDate, snapshot.EndDate?.ToBsonDate());

        try
        {
            await db.Subscriptions.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.IsDuplicateKey())
        {
            // versione già applicata (o più nuova), oppure abbonamento cancellato
        }
    }

    /// <summary>Tombstone: Version massima, così un SubscriptionUpdated in ritardo non lo resuscita.</summary>
    public Task MarkDeletedAsync(Guid subscriptionId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Subscriptions.UpdateOneAsync(
            Filter.Eq(d => d.SubscriptionId, subscriptionId),
            Update
                .Set(d => d.UserId, userId)
                .Set(d => d.Version, long.MaxValue)
                .Set(d => d.Deleted, true)
                .Set(d => d.ExpireAtUtc, (now + options.Value.DeletedTombstoneRetention).UtcDateTime),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);

    /// <summary>Prima il tombstone dell'utente (blocca gli eventi in ritardo), poi i suoi abbonamenti.</summary>
    public async Task DeleteAllForUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await db.DeletedUsers.ReplaceOneAsync(
            Builders<DeletedUserDocument>.Filter.Eq(d => d.UserId, userId),
            new DeletedUserDocument
            {
                UserId = userId,
                DeletedAtUtc = now.UtcDateTime,
                ExpireAtUtc = (now + options.Value.DeletedTombstoneRetention).UtcDateTime
            },
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        await db.Subscriptions.DeleteManyAsync(Filter.Eq(d => d.UserId, userId), cancellationToken);
    }

    public Task<bool> IsUserDeletedAsync(Guid userId, CancellationToken cancellationToken) =>
        db.DeletedUsers
            .Find(Builders<DeletedUserDocument>.Filter.Eq(d => d.UserId, userId))
            .AnyAsync(cancellationToken);

    /// <summary>Tutti gli abbonamenti dell'utente, anche terminati (servono al trend). Esclusi i tombstone.</summary>
    public async Task<IReadOnlyList<AnalyticsSubscription>> GetForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var documents = await db.Subscriptions
            .Find(Filter.Eq(d => d.UserId, userId) & Filter.Eq(d => d.Deleted, false))
            .ToListAsync(cancellationToken);

        return documents.Select(d => d.ToModel()).ToList();
    }
}