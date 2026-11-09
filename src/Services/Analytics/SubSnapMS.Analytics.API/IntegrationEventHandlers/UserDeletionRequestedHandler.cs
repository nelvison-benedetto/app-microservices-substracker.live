using SubSnapMS.Analytics.API.Persistence;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;

namespace SubSnapMS.Analytics.API.IntegrationEventHandlers;

//Saga di cancellazione: Analytics è un read model, cancella e NON risponde con UserDataDeleted
public sealed class UserDeletionRequestedHandler(
    SubscriptionReadStore store,
    TimeProvider timeProvider,
    ILogger<UserDeletionRequestedHandler> logger) : IIntegrationEventHandler<UserDeletionRequested>
{
    public async Task HandleAsync(UserDeletionRequested e, MessageContext context, CancellationToken cancellationToken)
    {
        await store.DeleteAllForUserAsync(e.UserId, timeProvider.GetUtcNow(), cancellationToken);
        logger.LogInformation("User deletion {UserId}: analytics data removed", e.UserId);
    }
}