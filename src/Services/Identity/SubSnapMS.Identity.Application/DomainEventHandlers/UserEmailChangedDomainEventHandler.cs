using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Identity.Core.Users.Events;

namespace SubSnapMS.Identity.Application.DomainEventHandlers;

internal sealed class UserEmailChangedDomainEventHandler(IIntegrationEventOutbox outbox)
    : IDomainEventHandler<UserEmailChangedDomainEvent>
{
    public Task HandleAsync(UserEmailChangedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new UserEmailChanged
        {
            UserId = domainEvent.UserId.Value,
            NewEmail = domainEvent.NewEmail.Value,
            ChangedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}