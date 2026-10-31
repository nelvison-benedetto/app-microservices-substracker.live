using Microsoft.EntityFrameworkCore;
using SubSnapMS.Subscriptions.Application.Abstractions.Persistence;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Infrastructure.Persistence.Repositories;

/// <summary>
/// Carica l'aggregate SENZA storico (append-only: le voci nuove vengono inserite al salvataggio,
/// quelle vecchie si leggono solo dal lato query).
/// </summary>
internal sealed class SubscriptionRepository(SubscriptionsDbContext dbContext) : ISubscriptionRepository
{
    public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken) =>
        dbContext.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Subscription subscription) => dbContext.Subscriptions.Add(subscription);

    /// <summary>Lo storico non caricato viene eliminato dalla cascade del database.</summary>
    public void Remove(Subscription subscription) => dbContext.Subscriptions.Remove(subscription);

    /// <summary>
    /// Una sola DELETE, eseguita subito (non aspetta SaveChanges): nella saga gira dentro la transazione
    /// esplicita dell'Inbox, quindi resta atomica con la conferma nell'Outbox.
    /// </summary>
    public Task<int> DeleteAllForUserAsync(UserId userId, CancellationToken cancellationToken) =>
        dbContext.Subscriptions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
}