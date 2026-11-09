using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.IntegrationEventHandlers;

public sealed class SubscriptionCreatedHandler(RecipientStore recipients, SubscriptionStore subscriptions)
    : IIntegrationEventHandler<SubscriptionCreated>
{
    public async Task HandleAsync(SubscriptionCreated e, MessageContext context, CancellationToken cancellationToken)
    {
        //Evento in ritardo di un utente già cancellato: non ricreare i suoi dati
        if (await recipients.IsDeletedAsync(e.UserId, cancellationToken))
            return;

        await subscriptions.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}