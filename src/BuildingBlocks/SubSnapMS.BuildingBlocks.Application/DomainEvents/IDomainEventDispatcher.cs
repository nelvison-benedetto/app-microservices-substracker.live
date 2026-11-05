using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.BuildingBlocks.Application.DomainEvents;

//Esegue tutti gli handler registrati per un domain event. Usato dal DbContext al salvataggio

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

