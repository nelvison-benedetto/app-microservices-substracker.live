using Microsoft.EntityFrameworkCore;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

//🔥🔥Tutto in UNA transazione: controllo duplicato → handler (SaveChanges, ExecuteDelete, Outbox...) → record Inbox → commit.
//Inbox sullo stesso DbContext dell'handler (scoped: stessa istanza).
//Se due istanze processano lo stesso messaggio insieme, la chiave primaria fa fallire la seconda, la sua transazione viene annullata e al retry il messaggio risulta già processato.


internal sealed class EfCoreInboxStore<TContext>(TContext dbContext) : IInboxStore
    where TContext : DbContext
{
    public Task<bool> ExecuteOnceAsync(
        Guid messageId,
        string handlerName,
        Func<CancellationToken, Task> handle,
        CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();  //necessario se il DbContext usa EnableRetryOnFailure: la transazione esplicita va nella execution strategy

        return strategy.ExecuteAsync(async token =>
        {
            dbContext.ChangeTracker.Clear();   //un eventuale tentativo precedente non lascia modifiche pendenti
            await using var transaction = await dbContext.Database.BeginTransactionAsync(token);  //apre transazione esplicita su connessione del DbContext

            var alreadyProcessed = await dbContext.Set<InboxMessage>()
                .AsNoTracking()
                .AnyAsync(m => m.MessageId == messageId && m.HandlerName == handlerName, token);  //“esiste già un record per questo messaggio e questo handler?”. AnyAsync diventa un SELECT EXISTS(…) e si ferma alla prima riga match.

            if (alreadyProcessed)
                return false;

            await handle(token);  //esegue l'handler, tramite la lambda passata

            dbContext.Set<InboxMessage>().Add(new InboxMessage(messageId, handlerName, DateTimeOffset.UtcNow));  //aggiunge il record 'fatto' al change tracker
            await dbContext.SaveChangesAsync(token);   //1 solo savechanges x all. salva anche le modifiche lasciate dall'handler
            await transaction.CommitAsync(token);  //Commit: da qui in poi tutto è definitivo, insieme
            return true;
        }, cancellationToken);
    }
}