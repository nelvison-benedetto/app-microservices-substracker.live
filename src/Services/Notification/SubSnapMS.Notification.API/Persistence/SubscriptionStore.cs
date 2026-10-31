using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SubSnapMS.Notification.API.Configuration;
using SubSnapMS.Notification.API.Persistence.Documents;
using SubSnapMS.Notification.API.Renewals;

namespace SubSnapMS.Notification.API.Persistence;

/// <summary>Stato completo di un abbonamento, preso da SubscriptionCreated/Updated.</summary>
public sealed record SubscriptionSnapshot(
    Guid SubscriptionId,
    Guid UserId,
    long Version,
    string Name,
    decimal Amount,
    string Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate);

public sealed class SubscriptionStore(NotificationDb db, LocalCalendar calendar, IOptions<NotificationOptions> options)
{
    private static FilterDefinitionBuilder<SubscriptionDocument> Filter => Builders<SubscriptionDocument>.Filter;
    private static UpdateDefinitionBuilder<SubscriptionDocument> Update => Builders<SubscriptionDocument>.Update;

    /// <summary>
    /// Applica lo stato solo se la Version è più nuova (eventi fuori ordine o duplicati vengono ignorati).
    /// ReminderPending torna true: se il promemoria per quella data era già stato inviato,
    /// il registro invii lo riconosce e non lo reinvia.
    /// </summary>
    public async Task UpsertAsync(SubscriptionSnapshot snapshot, CancellationToken cancellationToken)
    {
        var next = RenewalCalculator.NextRenewal(snapshot.StartDate, snapshot.BillingCycle, snapshot.EndDate, calendar.Today());

        var filter = Filter.Eq(d => d.SubscriptionId, snapshot.SubscriptionId)
                     & Filter.Lt(d => d.Version, snapshot.Version);

        var update = Update
            .Set(d => d.UserId, snapshot.UserId)
            .Set(d => d.Version, snapshot.Version)
            .Set(d => d.Deleted, false)
            .Set(d => d.Name, snapshot.Name)
            .Set(d => d.Amount, snapshot.Amount)
            .Set(d => d.Currency, snapshot.Currency)
            .Set(d => d.BillingCycle, snapshot.BillingCycle)
            .Set(d => d.StartDate, snapshot.StartDate.ToBsonDate())
            .Set(d => d.EndDate, snapshot.EndDate?.ToBsonDate())
            .Set(d => d.NextRenewalDate, next?.ToBsonDate())
            .Set(d => d.ReminderPending, next is not null);

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
                .Set(d => d.NextRenewalDate, null)
                .Set(d => d.ReminderPending, false)
                .Set(d => d.ExpireAtUtc, (now + options.Value.DeletedTombstoneRetention).UtcDateTime),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);

    public Task DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Subscriptions.DeleteManyAsync(Filter.Eq(d => d.UserId, userId), cancellationToken);

    /// <summary>
    /// Porta avanti le date di rinnovo già passate (il rinnovo di ieri → il prossimo).
    /// L'aggiornamento è condizionato alla Version letta: se nel frattempo è arrivato un evento, vince l'evento.
    /// </summary>
    public async Task<int> AdvanceStaleRenewalsAsync(DateOnly today, int batchSize, CancellationToken cancellationToken)
    {
        var stale = await db.Subscriptions
            .Find(Filter.Eq(d => d.Deleted, false) & Filter.Lt(d => d.NextRenewalDate, today.ToBsonDate()))
            .Limit(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var subscription in stale)
        {
            var next = RenewalCalculator.NextRenewal(
                subscription.StartDate.ToDateOnly(),
                subscription.BillingCycle,
                subscription.EndDate?.ToDateOnly(),
                today);

            await db.Subscriptions.UpdateOneAsync(
                Filter.Eq(d => d.SubscriptionId, subscription.SubscriptionId) & Filter.Eq(d => d.Version, subscription.Version),
                Update
                    .Set(d => d.NextRenewalDate, next?.ToBsonDate())
                    .Set(d => d.ReminderPending, next is not null),
                cancellationToken: cancellationToken);
        }

        return stale.Count;
    }

    /// <summary>Abbonamenti con un rinnovo tra from e to (inclusi) e promemoria non ancora gestito.</summary>
    public Task<List<SubscriptionDocument>> FindDueRemindersAsync(DateOnly from, DateOnly to, int batchSize, CancellationToken cancellationToken) =>
        db.Subscriptions
            .Find(Filter.Eq(d => d.ReminderPending, true)
                  & Filter.Eq(d => d.Deleted, false)
                  & Filter.Gte(d => d.NextRenewalDate, from.ToBsonDate())
                  & Filter.Lte(d => d.NextRenewalDate, to.ToBsonDate()))
            .SortBy(d => d.NextRenewalDate)
            .Limit(batchSize)
            .ToListAsync(cancellationToken);

    /// <summary>Solo se la data di rinnovo non è cambiata nel frattempo.</summary>
    public Task MarkReminderHandledAsync(Guid subscriptionId, DateOnly renewalDate, CancellationToken cancellationToken) =>
        db.Subscriptions.UpdateOneAsync(
            Filter.Eq(d => d.SubscriptionId, subscriptionId) & Filter.Eq(d => d.NextRenewalDate, renewalDate.ToBsonDate()),
            Update.Set(d => d.ReminderPending, false),
            cancellationToken: cancellationToken);
}