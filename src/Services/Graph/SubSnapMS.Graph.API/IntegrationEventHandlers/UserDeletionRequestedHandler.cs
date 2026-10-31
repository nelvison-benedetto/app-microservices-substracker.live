using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Graph.API.Projection;

namespace SubSnapMS.Graph.API.IntegrationEventHandlers;

/// <summary>Saga di cancellazione: Graph è un read model, cancella e NON risponde con UserDataDeleted.</summary>
public sealed class UserDeletionRequestedHandler(GraphProjector projector) : IIntegrationEventHandler<UserDeletionRequested>
{
    public Task HandleAsync(UserDeletionRequested e, MessageContext context, CancellationToken cancellationToken) =>
        projector.DeleteAllForUserAsync(e.UserId, cancellationToken);
}