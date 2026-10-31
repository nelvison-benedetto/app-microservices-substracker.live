using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Search.API.Indexing;

namespace SubSnapMS.Search.API.IntegrationEventHandlers;

public sealed class SubscriptionUpdatedHandler(SubscriptionIndexer indexer) : IIntegrationEventHandler<SubscriptionUpdated>
{
    public async Task HandleAsync(SubscriptionUpdated e, MessageContext context, CancellationToken cancellationToken)
    {
        if (await indexer.IsUserDeletedAsync(e.UserId, cancellationToken))
            return;

        await indexer.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Category, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}