using Microsoft.EntityFrameworkCore;
using SubSnapMS.Subscriptions.Application.Abstractions.Persistence;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Infrastructure.Persistence.Repositories;

//Carica l'aggregate SENZA storico (append-only: le voci nuove vengono inserite al salvataggio, quelle vecchie si leggono solo dal lato query)
internal sealed class SubscriptionRepository(SubscriptionsDbContext dbContext) : ISubscriptionRepository
{
    public Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken) =>
        dbContext.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Subscription subscription) => dbContext.Subscriptions.Add(subscription);

    //Lo storico non caricato viene eliminato dalla cascade del database
    public void Remove(Subscription subscription) => dbContext.Subscriptions.Remove(subscription);

    //una sola DELETE, eseguita subito (non aspetta SaveChanges): nella saga gira dentro la transazione esplicita dell'Inbox, quindi resta atomica con la conferma nell'Outbox.
    public Task<int> DeleteAllForUserAsync(UserId userId, CancellationToken cancellationToken) =>
        dbContext.Subscriptions
            .Where(s => s.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
}