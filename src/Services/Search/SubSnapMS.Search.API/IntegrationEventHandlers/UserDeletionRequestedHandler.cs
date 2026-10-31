using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Search.API.Indexing;

namespace SubSnapMS.Search.API.IntegrationEventHandlers;

/// <summary>Saga di cancellazione: Search è un read model, cancella e NON risponde con UserDataDeleted.</summary>
public sealed class UserDeletionRequestedHandler(SubscriptionIndexer indexer) : IIntegrationEventHandler<UserDeletionRequested>
{
    public Task HandleAsync(UserDeletionRequested e, MessageContext context, CancellationToken cancellationToken) =>
        indexer.DeleteAllForUserAsync(e.UserId, cancellationToken);
}