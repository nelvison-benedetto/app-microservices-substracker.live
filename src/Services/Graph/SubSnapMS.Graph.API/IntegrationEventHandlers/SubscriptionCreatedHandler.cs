using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Graph.API.Projection;

namespace SubSnapMS.Graph.API.IntegrationEventHandlers;

public sealed class SubscriptionCreatedHandler(GraphProjector projector) : IIntegrationEventHandler<SubscriptionCreated>
{
    public async Task HandleAsync(SubscriptionCreated e, MessageContext context, CancellationToken cancellationToken)
    {
        //Evento in ritardo di un utente già cancellato: non ricreare i suoi nodi
        if (await projector.IsUserDeletedAsync(e.UserId, cancellationToken))
            return;

        await projector.UpsertAsync(
            new SubscriptionSnapshot(e.SubscriptionId, e.UserId, e.Version, e.Name, e.Category, e.Amount, e.Currency, e.BillingCycle, e.StartDate, e.EndDate),
            cancellationToken);
    }
}