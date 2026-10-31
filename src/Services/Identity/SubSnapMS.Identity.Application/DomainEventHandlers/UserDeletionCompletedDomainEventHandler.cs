using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Identity.Core.Deletions.Events;

namespace SubSnapMS.Identity.Application.DomainEventHandlers;

internal sealed class UserDeletionCompletedDomainEventHandler(IIntegrationEventOutbox outbox)
    : IDomainEventHandler<UserDeletionCompletedDomainEvent>
{
    public Task HandleAsync(UserDeletionCompletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new UserDeletionCompleted
        {
            UserId = domainEvent.UserId.Value,
            CompletedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}