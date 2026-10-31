using SubSnapMS.Analytics.API.Persistence;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Analytics.API.IntegrationEventHandlers;

public sealed class SubscriptionUpdatedHandler(SubscriptionReadStore store) : IIntegrationEventHandler<SubscriptionUpdated>
{
    public async Task HandleAsync(SubscriptionUpdated e, MessageContext context, CancellationToken cancellationToken)
    {
        if (await store.IsUserDeletedAsync(e.UserId, cancellationToken))
            return;

        await store.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Category, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}