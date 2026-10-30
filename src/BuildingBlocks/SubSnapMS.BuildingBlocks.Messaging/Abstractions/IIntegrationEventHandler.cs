using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Handler di un integration event ricevuto da RabbitMQ.
/// Regole:
///  - con l'Inbox EF attiva NON chiamare SaveChanges: lo fa la pipeline, insieme al record Inbox (una transazione);
///  - senza Inbox, l'handler deve essere idempotente (upsert per id: Mongo, Elasticsearch, Neo4j);
///  - un'eccezione = retry con backoff, poi Dead Letter Queue.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);
}
