using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

//Pubblicazione DIRETTA su RabbitMQ, senza Outbox.
//Usala solo quando non c'è una scrittura su database da rendere atomica con l'evento. Se scrivi dati E pubblichi un evento, usa IIntegrationEventOutbox.

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}