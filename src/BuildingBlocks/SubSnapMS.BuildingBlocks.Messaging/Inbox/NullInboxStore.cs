using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// Nessuna Inbox: per i servizi senza EF (Search, Analytics, Graph su Elasticsearch/Mongo/Neo4j),
/// dove l'idempotenza si ottiene scrivendo con upsert per id.
/// </summary>
internal sealed class NullInboxStore : IInboxStore
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, string handlerName, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    public Task MarkProcessedAsync(Guid messageId, string handlerName, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
