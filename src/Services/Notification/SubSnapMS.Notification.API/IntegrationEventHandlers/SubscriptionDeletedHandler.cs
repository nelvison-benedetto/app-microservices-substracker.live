using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.IntegrationEventHandlers;

public sealed class SubscriptionDeletedHandler(SubscriptionStore subscriptions, TimeProvider timeProvider)
    : IIntegrationEventHandler<SubscriptionDeleted>
{
    public Task HandleAsync(SubscriptionDeleted e, MessageContext context, CancellationToken cancellationToken) =>
        subscriptions.MarkDeletedAsync(e.SubscriptionId, e.UserId, timeProvider.GetUtcNow(), cancellationToken);
}