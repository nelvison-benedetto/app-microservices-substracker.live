using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Pubblicazione DIRETTA su RabbitMQ, senza Outbox.
/// Usala solo quando non c'è una scrittura su database da rendere atomica con l'evento.
/// Se scrivi dati E pubblichi un evento, usa IIntegrationEventOutbox.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;
}