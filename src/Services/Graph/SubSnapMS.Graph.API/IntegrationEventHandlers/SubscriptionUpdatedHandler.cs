using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Graph.API.Projection;

namespace SubSnapMS.Graph.API.IntegrationEventHandlers;

public sealed class SubscriptionUpdatedHandler(GraphProjector projector) : IIntegrationEventHandler<SubscriptionUpdated>
{
    public async Task HandleAsync(SubscriptionUpdated e, MessageContext context, CancellationToken cancellationToken)
    {
        if (await projector.IsUserDeletedAsync(e.UserId, cancellationToken))
            return;

        await projector.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Category, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}