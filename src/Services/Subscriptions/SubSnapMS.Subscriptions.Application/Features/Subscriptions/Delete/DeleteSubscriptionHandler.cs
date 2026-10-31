using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Abstractions.Persistence;
using SubSnapMS.Subscriptions.Core.Subscriptions.Exceptions;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Delete;

/// <summary>
/// Cancella l'abbonamento (lo storico in cascade). Delete() genera SubscriptionDeletedDomainEvent →
/// SubscriptionDeleted nell'Outbox: i read model (Analytics, Search, Graph) rimuovono la loro copia.
/// </summary>
internal sealed class DeleteSubscriptionHandler(
    ISubscriptionRepository repository,
    TimeProvider timeProvider) : ICommandHandler<DeleteSubscriptionCommand>
{
    public async Task Handle(DeleteSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await repository.GetByIdAsync(command.SubscriptionId, cancellationToken)
                           ?? throw new SubscriptionNotFoundException();

        subscription.EnsureOwnedBy(command.UserId);

        subscription.Delete(timeProvider.GetUtcNow());
        repository.Remove(subscription);
    }
}