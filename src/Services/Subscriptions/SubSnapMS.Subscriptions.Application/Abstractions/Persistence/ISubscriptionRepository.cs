using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Abstractions.Persistence;

//Repository dell'aggregate Subscription. Lo storico NON viene caricato: è append-only (le nuove voci aggiunte dall'aggregate vengono inserite) e si legge solo dal lato query.
public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken);

    void Add(Subscription subscription);

    void Remove(Subscription subscription);

    //Cancellazione massiva (saga di cancellazione account): una sola istruzione SQL, lo storico viene eliminato in cascade dal database. Non genera domain event.
    Task<int> DeleteAllForUserAsync(UserId userId, CancellationToken cancellationToken);
}