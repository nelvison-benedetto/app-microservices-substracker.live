using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Handler di un integration event ricevuto da RabbitMQ.
/// Regole:
///  - con l'Inbox EF attiva, l'handler gira dentro una transazione: SaveChanges, ExecuteDelete,
///    ExecuteUpdate e IIntegrationEventOutbox.Add sono tutti committati insieme al record Inbox;
///  - senza Inbox (servizi su Mongo/Elasticsearch/Neo4j) l'handler deve essere idempotente: upsert per id;
///  - un'eccezione = retry con backoff esponenziale, poi Dead Letter Queue.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);
}