using SubSnapMS.Analytics.API.Persistence;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Analytics.API.IntegrationEventHandlers;

public sealed class SubscriptionDeletedHandler(SubscriptionReadStore store, TimeProvider timeProvider)
    : IIntegrationEventHandler<SubscriptionDeleted>
{
    public Task HandleAsync(SubscriptionDeleted e, MessageContext context, CancellationToken cancellationToken) =>
        store.MarkDeletedAsync(e.SubscriptionId, e.UserId, timeProvider.GetUtcNow(), cancellationToken);
}