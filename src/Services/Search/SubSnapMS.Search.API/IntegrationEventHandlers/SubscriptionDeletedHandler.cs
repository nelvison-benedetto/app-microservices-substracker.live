using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Search.API.Indexing;

namespace SubSnapMS.Search.API.IntegrationEventHandlers;

public sealed class SubscriptionDeletedHandler(SubscriptionIndexer indexer) : IIntegrationEventHandler<SubscriptionDeleted>
{
    public Task HandleAsync(SubscriptionDeleted e, MessageContext context, CancellationToken cancellationToken) =>
        indexer.MarkDeletedAsync(e.SubscriptionId, e.UserId, cancellationToken);
}