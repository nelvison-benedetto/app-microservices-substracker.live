using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Abstractions.Persistence;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Subscriptions;
using SubSnapMS.Subscriptions.Core.Subscriptions.Exceptions;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Update;

//Modifica l'abbonamento. Se nessun campo cambia, non succede nulla (niente storico, niente evento).
//Version: controllata dal dominio (ExpectedVersion → 409) e dal database (concurrency token → 409).
internal sealed class UpdateSubscriptionHandler(
    ISubscriptionRepository repository,
    TimeProvider timeProvider) : ICommandHandler<UpdateSubscriptionCommand, SubscriptionResult>
{
    public async Task<SubscriptionResult> Handle(UpdateSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetByIdAsync(command.SubscriptionId, cancellationToken) ?? throw new SubscriptionNotFoundException();

        //Di un altro utente → 404, come se non esistesse
        subscription.EnsureOwnedBy(command.UserId);

        var details = SubscriptionDetails.Create(
            command.Name,
            command.Category,
            Money.Create(command.Amount, command.Currency ?? subscription.Details.Price.Currency),
            BillingCycleExtensions.Parse(command.BillingCycle),
            SubscriptionPeriod.Create(command.StartDate, command.EndDate));

        subscription.Update(details, command.ExpectedVersion, timeProvider.GetUtcNow());

        return SubscriptionResult.From(subscription);
    }
}