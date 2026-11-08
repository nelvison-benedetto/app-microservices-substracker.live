using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Abstractions.Queries;

//Lato lettura (CQRS): AsNoTracking, risultati già nel formato dei DTO
public interface ISubscriptionQueries
{
    //Abbonamenti dell'utente, dal più recente
    Task<IReadOnlyList<SubscriptionResult>> GetUserSubscriptionsAsync(UserId userId, CancellationToken cancellationToken);

    //Dettaglio + storico (dal più recente). Null se non esiste O non appartiene all'utente
    Task<SubscriptionDetailResult?> GetDetailAsync(UserId userId, SubscriptionId subscriptionId, CancellationToken cancellationToken);
}