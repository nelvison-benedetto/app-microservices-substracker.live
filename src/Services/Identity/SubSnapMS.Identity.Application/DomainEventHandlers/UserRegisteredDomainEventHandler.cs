using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Identity.Core.Users.Events;

namespace SubSnapMS.Identity.Application.DomainEventHandlers;

internal sealed class UserRegisteredDomainEventHandler(IIntegrationEventOutbox outbox)
    : IDomainEventHandler<UserRegisteredDomainEvent>
{
    public Task HandleAsync(UserRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new UserRegistered
        {
            UserId = domainEvent.UserId.Value,
            Email = domainEvent.Email.Value,
            RegisteredOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}