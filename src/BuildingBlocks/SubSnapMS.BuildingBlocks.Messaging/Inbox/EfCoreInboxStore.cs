using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

/// <summary>
/// Inbox sullo stesso DbContext dell'handler (scoped: stessa istanza).
/// Tutto in UNA transazione: controllo duplicato → handler (SaveChanges, ExecuteDelete, Outbox...) → record Inbox → commit.
/// Se due istanze processano lo stesso messaggio insieme, la chiave primaria fa fallire la seconda,
/// la sua transazione viene annullata e al retry il messaggio risulta già processato.
/// </summary>
internal sealed class EfCoreInboxStore<TContext>(TContext dbContext) : IInboxStore
    where TContext : DbContext
{
    public Task<bool> ExecuteOnceAsync(
        Guid messageId,
        string handlerName,
        Func<CancellationToken, Task> handle,
        CancellationToken cancellationToken)
    {
        // Necessario se il DbContext usa EnableRetryOnFailure: la transazione esplicita va nella execution strategy
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async token =>
        {
            dbContext.ChangeTracker.Clear();   // un eventuale tentativo precedente non lascia modifiche pendenti
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);

            var alreadyProcessed = await dbContext.Set<InboxMessage>()
                .AsNoTracking()
                .AnyAsync(m => m.MessageId == messageId && m.HandlerName == handlerName, token);

            if (alreadyProcessed)
                return false;

            await handle(token);

            dbContext.Set<InboxMessage>().Add(new InboxMessage(messageId, handlerName, DateTimeOffset.UtcNow));
            await dbContext.SaveChangesAsync(token);   // salva anche le modifiche lasciate dall'handler
            await transaction.CommitAsync(token);

            return true;
        }, cancellationToken);
    }
}