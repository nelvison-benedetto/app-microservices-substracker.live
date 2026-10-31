using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Abstractions.Queries;

/// <summary>Lato lettura (CQRS): AsNoTracking, risultati già nel formato dei DTO.</summary>
public interface ISubscriptionQueries
{
    /// <summary>Abbonamenti dell'utente, dal più recente.</summary>
    Task<IReadOnlyList<SubscriptionResult>> GetUserSubscriptionsAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>Dettaglio + storico (dal più recente). Null se non esiste O non appartiene all'utente.</summary>
    Task<SubscriptionDetailResult?> GetDetailAsync(UserId userId, SubscriptionId subscriptionId, CancellationToken cancellationToken);
}