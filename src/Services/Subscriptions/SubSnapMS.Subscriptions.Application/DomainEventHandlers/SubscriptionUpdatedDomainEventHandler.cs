using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Subscriptions.Core.Subscriptions;
using SubSnapMS.Subscriptions.Core.Subscriptions.Events;

namespace SubSnapMS.Subscriptions.Application.DomainEventHandlers;

/// <summary>Stato COMPLETO dopo la modifica: i consumer fanno un upsert e scartano le version vecchie.</summary>
internal sealed class SubscriptionUpdatedDomainEventHandler(IIntegrationEventOutbox outbox)
    : IDomainEventHandler<SubscriptionUpdatedDomainEvent>
{
    public Task HandleAsync(SubscriptionUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var details = domainEvent.Details;

        outbox.Add(new SubscriptionUpdated
        {
            SubscriptionId = domainEvent.SubscriptionId.Value,
            UserId = domainEvent.UserId.Value,
            Version = domainEvent.Version,
            Name = details.Name,
            Category = details.Category,
            Amount = details.Price.Amount,
            Currency = details.Price.Currency,
            BillingCycle = details.BillingCycle.ToCode(),
            StartDate = details.Period.StartDate,
            EndDate = details.Period.EndDate,
            UpdatedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}