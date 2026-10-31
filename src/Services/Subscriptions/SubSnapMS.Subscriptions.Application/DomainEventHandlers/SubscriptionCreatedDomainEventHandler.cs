using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Subscriptions.Core.Subscriptions;
using SubSnapMS.Subscriptions.Core.Subscriptions.Events;

namespace SubSnapMS.Subscriptions.Application.DomainEventHandlers;

internal sealed class SubscriptionCreatedDomainEventHandler(IIntegrationEventOutbox outbox)
    : IDomainEventHandler<SubscriptionCreatedDomainEvent>
{
    public Task HandleAsync(SubscriptionCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var details = domainEvent.Details;

        outbox.Add(new SubscriptionCreated
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
            CreatedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}