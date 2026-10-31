using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.IntegrationEventHandlers;

public sealed class SubscriptionUpdatedHandler(RecipientStore recipients, SubscriptionStore subscriptions)
    : IIntegrationEventHandler<SubscriptionUpdated>
{
    public async Task HandleAsync(SubscriptionUpdated e, MessageContext context, CancellationToken cancellationToken)
    {
        if (await recipients.IsDeletedAsync(e.UserId, cancellationToken))
            return;

        await subscriptions.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}