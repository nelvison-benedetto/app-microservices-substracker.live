using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.BuildingBlocks.Application.DomainEvents;

//Reagisce a un domain event DURANTE il salvataggio, nella stessa transazione.
//Regole: non chiamare SaveChanges; puoi modificare aggregate, aggiungere integration event all'Outbox o far nascere altri domain event (verranno gestiti nello stesso salvataggio).
//Registrazione automatica: basta implementare l'interfaccia nell'assembly .Application del servizio.

public interface IDomainEventHandler<in TDomainEvent> where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}