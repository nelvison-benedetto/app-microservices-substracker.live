using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Search.API.Indexing;

namespace SubSnapMS.Search.API.IntegrationEventHandlers;

public sealed class SubscriptionCreatedHandler(SubscriptionIndexer indexer) : IIntegrationEventHandler<SubscriptionCreated>
{
    public async Task HandleAsync(SubscriptionCreated e, MessageContext context, CancellationToken cancellationToken)
    {
        // Evento in ritardo di un utente già cancellato: non reindicizzare i suoi dati
        if (await indexer.IsUserDeletedAsync(e.UserId, cancellationToken))
            return;

        await indexer.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Category, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}