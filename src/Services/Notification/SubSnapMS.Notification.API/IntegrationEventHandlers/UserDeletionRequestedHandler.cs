using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.IntegrationEventHandlers;

//Saga di cancellazione: Notification è un read model, quindi cancella e NON risponde con UserDataDeleted.
//Prima il tombstone (blocca gli eventi in ritardo), poi abbonamenti e registro invii. Tutto idempotente.
public sealed class UserDeletionRequestedHandler(
    RecipientStore recipients,
    SubscriptionStore subscriptions,
    NotificationLog notificationLog,
    TimeProvider timeProvider,
    ILogger<UserDeletionRequestedHandler> logger) : IIntegrationEventHandler<UserDeletionRequested>
{
    public async Task HandleAsync(UserDeletionRequested integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var userId = integrationEvent.UserId;

        await recipients.MarkDeletedAsync(userId, timeProvider.GetUtcNow(), cancellationToken);
        await subscriptions.DeleteAllForUserAsync(userId, cancellationToken);
        await notificationLog.DeleteAllForUserAsync(userId, cancellationToken);

        logger.LogInformation("User deletion {UserId}: notification data removed", userId);
    }
}