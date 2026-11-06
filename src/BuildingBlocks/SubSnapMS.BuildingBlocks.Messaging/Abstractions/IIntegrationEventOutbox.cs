using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

//Accoda un evento nell'Outbox del DbContext corrente. NON salva: l'evento viene scritto dal prossimo SaveChanges (il TransactionBehavior), nella stessa transazione dei dati.

public interface IIntegrationEventOutbox
{
    void Add<TEvent>(TEvent integrationEvent) where TEvent : IntegrationEvent;
}