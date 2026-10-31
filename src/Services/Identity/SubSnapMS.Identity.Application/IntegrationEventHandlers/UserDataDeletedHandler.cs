using Microsoft.Extensions.Logging;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.UserDeletion;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.IntegrationEventHandlers;

/// <summary>
/// Orchestratore della saga: registra la conferma di un servizio; quando sono arrivate tutte,
/// rimuove l'utente e chiude il processo (→ UserDeletionCompleted).
/// Gira dentro la transazione dell'Inbox: niente SaveChanges qui, salva l'Inbox
/// (e il salvataggio esegue anche i domain event handler).
/// Public: registrato da Program.cs con messaging.AddConsumer&lt;UserDataDeleted, UserDataDeletedHandler&gt;().
/// </summary>
public sealed class UserDataDeletedHandler(
    IUserDeletionRepository deletions,
    IUserRepository users,
    TimeProvider timeProvider,
    ILogger<UserDataDeletedHandler> logger) : IIntegrationEventHandler<UserDataDeleted>
{
    public async Task HandleAsync(UserDataDeleted integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var userId = UserId.From(integrationEvent.UserId);

        var deletion = await deletions.GetByIdAsync(userId, cancellationToken);
        if (deletion is null)
        {
            logger.LogWarning("UserDataDeleted from {Service} for unknown deletion {UserId}: ignored", integrationEvent.Service, userId);
            return;
        }

        if (!deletion.Confirm(integrationEvent.Service))
            return;   // duplicato o servizio non atteso

        logger.LogInformation("User deletion {UserId}: {Service} confirmed ({Pending} pending)",
            userId, integrationEvent.Service, deletion.PendingServices.Count);

        if (!deletion.AllServicesConfirmed)
            return;

        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is not null)
            users.Remove(user);

        deletion.Complete(timeProvider.GetUtcNow());
    }
}