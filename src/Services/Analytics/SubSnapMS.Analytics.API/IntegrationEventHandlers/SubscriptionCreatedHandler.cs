using SubSnapMS.Analytics.API.Persistence;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Analytics.API.IntegrationEventHandlers;

public sealed class SubscriptionCreatedHandler(SubscriptionReadStore store) : IIntegrationEventHandler<SubscriptionCreated>
{
    public async Task HandleAsync(SubscriptionCreated e, MessageContext context, CancellationToken cancellationToken)
    {
        //Evento in ritardo di un utente già cancellato: non ricreare i suoi dati
        if (await store.IsUserDeletedAsync(e.UserId, cancellationToken))
            return;

        await store.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Category, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}