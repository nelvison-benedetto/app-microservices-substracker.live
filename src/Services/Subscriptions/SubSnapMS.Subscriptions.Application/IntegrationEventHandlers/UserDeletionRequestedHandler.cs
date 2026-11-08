using Microsoft.Extensions.Logging;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Contracts.UserDeletion;
using SubSnapMS.Subscriptions.Application.Abstractions.Persistence;
using SubSnapMS.Subscriptions.Application.Common.Caching;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Application.IntegrationEventHandlers;

//Partecipante alla saga di cancellazione account: elimina tutti gli abbonamenti dell'utente (e lo storico) e conferma a Identity con UserDataDeleted. Gira dentro la transazione dell'Inbox: cancellazione + conferma nell'Outbox sono atomiche, e un duplicato del messaggio viene ignorato.
//Public: registrato da Program.cs con messaging.AddConsumer&lt;UserDeletionRequested, UserDeletionRequestedHandler&gt;().
public sealed class UserDeletionRequestedHandler(
    ISubscriptionRepository repository,
    IIntegrationEventOutbox outbox,
    UserSubscriptionsCache cache,
    TimeProvider timeProvider,
    ILogger<UserDeletionRequestedHandler> logger) : IIntegrationEventHandler<UserDeletionRequested>
{
    public async Task HandleAsync(UserDeletionRequested integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var userId = UserId.From(integrationEvent.UserId);

        var deleted = await repository.DeleteAllForUserAsync(userId, cancellationToken);

        outbox.Add(new UserDataDeleted
        {
            UserId = integrationEvent.UserId,
            Service = ServiceNames.Subscriptions,
            DeletedOnUtc = timeProvider.GetUtcNow()
        });

        //Prima del commit, ma qui va bene: l'account è già bloccato, nessuno rilegge la lista
        await cache.InvalidateAsync(userId, cancellationToken);

        logger.LogInformation("User deletion {UserId}: {Count} subscriptions removed", userId, deleted);
    }
}