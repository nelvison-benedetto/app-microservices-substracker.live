using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Subscriptions.Core.Subscriptions.Events;

namespace SubSnapMS.Subscriptions.Application.DomainEventHandlers;

internal sealed class SubscriptionDeletedDomainEventHandler(IIntegrationEventOutbox outbox)
    : IDomainEventHandler<SubscriptionDeletedDomainEvent>
{
    public Task HandleAsync(SubscriptionDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new SubscriptionDeleted
        {
            SubscriptionId = domainEvent.SubscriptionId.Value,
            UserId = domainEvent.UserId.Value,
            DeletedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}