using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Abstractions.Persistence;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Create;

/// <summary>
/// Crea l'abbonamento (con la prima voce di storico "created").
/// Al salvataggio: SubscriptionCreatedDomainEvent → SubscriptionCreated nell'Outbox. Dopo il commit: cache invalidata.
/// </summary>
internal sealed class CreateSubscriptionHandler(
    ISubscriptionRepository repository,
    TimeProvider timeProvider) : ICommandHandler<CreateSubscriptionCommand, SubscriptionResult>
{
    public Task<SubscriptionResult> Handle(CreateSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var details = SubscriptionDetails.Create(
            command.Name,
            command.Category,
            Money.Create(command.Amount, command.Currency ?? Money.DefaultCurrency),
            BillingCycleExtensions.Parse(command.BillingCycle),
            SubscriptionPeriod.Create(command.StartDate, command.EndDate));

        var subscription = Subscription.Create(command.UserId, details, timeProvider.GetUtcNow());
        repository.Add(subscription);

        return Task.FromResult(SubscriptionResult.From(subscription));
    }
}