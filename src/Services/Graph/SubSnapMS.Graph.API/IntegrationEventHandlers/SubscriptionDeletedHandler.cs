using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Graph.API.Projection;

namespace SubSnapMS.Graph.API.IntegrationEventHandlers;

public sealed class SubscriptionDeletedHandler(GraphProjector projector) : IIntegrationEventHandler<SubscriptionDeleted>
{
    public Task HandleAsync(SubscriptionDeleted e, MessageContext context, CancellationToken cancellationToken) =>
        projector.MarkDeletedAsync(e.SubscriptionId, e.UserId, cancellationToken);
}