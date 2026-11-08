using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Options;
using SubSnapMS.Identity.Core.Deletions;
using SubSnapMS.Identity.Core.Users.Events;

namespace SubSnapMS.Identity.Application.DomainEventHandlers;

//Avvio della saga: crea il processo UserDeletion (servizi attesi) e avvisa tutti i servizi. Se nessun servizio deve confermare, la saga si chiude subito (rimozione dell'utente).
internal sealed class UserDeletionRequestedDomainEventHandler(
    IUserDeletionRepository deletions,
    IUserRepository users,
    IIntegrationEventOutbox outbox,
    IOptions<UserDeletionOptions> options) : IDomainEventHandler<UserDeletionRequestedDomainEvent>
{
    public async Task HandleAsync(UserDeletionRequestedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new UserDeletionRequested
        {
            UserId = domainEvent.UserId.Value,
            RequestedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        if (await deletions.GetByIdAsync(domainEvent.UserId, cancellationToken) is not null)
            return;   // processo già avviato

        var deletion = UserDeletion.Start(domainEvent.UserId, options.Value.ExpectedServices, domainEvent.OccurredOnUtc);
        deletions.Add(deletion);

        if (!deletion.AllServicesConfirmed)
            return;

        var user = await users.GetByIdAsync(domainEvent.UserId, cancellationToken);
        if (user is not null)
            users.Remove(user);

        deletion.Complete(domainEvent.OccurredOnUtc);
    }
}