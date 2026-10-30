using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// Inbox sullo stesso DbContext dell'handler (scoped: stessa istanza).
/// MarkProcessedAsync fa UN SaveChanges: modifiche dell'handler + record Inbox in una transazione.
/// Se due istanze processano lo stesso messaggio insieme, la chiave primaria fa fallire la seconda
/// e le sue modifiche vengono annullate; al retry, HasBeenProcessedAsync restituisce true.
/// </summary>
internal sealed class EfCoreInboxStore<TContext>(TContext dbContext) : IInboxStore
    where TContext : DbContext
{
    public Task<bool> HasBeenProcessedAsync(Guid messageId, string handlerName, CancellationToken cancellationToken) =>
        dbContext.Set<InboxMessage>()
            .AsNoTracking()
            .AnyAsync(m => m.MessageId == messageId && m.HandlerName == handlerName, cancellationToken);

    public async Task MarkProcessedAsync(Guid messageId, string handlerName, CancellationToken cancellationToken)
    {
        dbContext.Set<InboxMessage>().Add(new InboxMessage(messageId, handlerName, DateTimeOffset.UtcNow));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}