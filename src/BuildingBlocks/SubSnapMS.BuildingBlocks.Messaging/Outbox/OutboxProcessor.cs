using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Diagnostics;
using SubSnapMS.BuildingBlocks.Messaging.RabbitMq;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Outbox;

//è il background process che ogni 2 seconds fa polling, e prende le ultime X  di tabella outbox_messages e 🔥LE PUBBLICA su RabbitMQ🔥
//🔥🔥Transactional Outbox = EfCoreOutbox.cs(scrive le rows nella transazione dati) + questo file!!

//Con più repliche del servizio, FOR UPDATE SKIP LOCKED fa sì che ogni riga venga presa da una sola replica.
//Garanzia: at-least-once (se il commit fallisce dopo la publish, il messaggio verrà ripubblicato; l'Inbox dei consumer scarta il duplicato).

internal sealed class OutboxProcessor<TContext>(
    IServiceScopeFactory scopeFactory,
    IMessagePublisher publisher,
    IOptions<OutboxOptions> options,
    ILogger<OutboxProcessor<TContext>> logger) : BackgroundService
    where TContext : DbContext
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var selectSql = BuildSelectSql();   //fallisce subito se il DbContext non mappa la tabella

        logger.LogInformation("Outbox processor started for {DbContext} (every {Interval})", typeof(TContext).Name, settings.PollingInterval);

        using var timer = new PeriodicTimer(settings.PollingInterval);  //PeriodicTimer è il timer moderno di .NET per i cicli asincroni.
        var lastCleanup = DateTimeOffset.MinValue;  //“la pulizia non è mai stata fatta”, cosi la prima pulizia avviene al primo giro

        try  //gestisce lo shutdown: quando l'app si ferma stoppingToken viene annullato, e WaitForNextTickAsync (o un’operazione in corso) lancia OperationCanceledException che viene intanto ignorata xk il servizio è finito.
        {
            do  //cycle. 1 cycle eseguito subito all'avvio, e poi i prossimi ogni 2sec
            {
                try  //protegge il ciclo. altrimenti un cycle fallisce allor avverebbe subito il catch e questo servizio in background che fa polling si fermerebbe x sempre.
                {
                    //Se il batch era pieno e tutto è andato bene, ce n'è probabilmente altro: continua subito
                    (int Fetched, int Failed) result;
                    do  //cycle x smaltire i picchi
                    {
                        result = await ProcessBatchAsync(selectSql, settings, stoppingToken);  //stoppingToken viene annullato, e WaitForNextTickAsync (o un’operazione in corso) lancia OperationCanceledException
                    }
                    while (result.Fetched == settings.BatchSize && result.Failed == 0 && !stoppingToken.IsCancellationRequested);  //si prende un'altro batch, solo se: il batch era pieno (50/50, significa che ci sono mexs in coda), nessuno è fallito(se fallisce qualcosa e.g. rabbitmq, aspetta il tic successivo invece di insistere), 
                    //con e.g. 500eventi in coda, il processo fa 10 batch

                    if (DateTimeOffset.UtcNow - lastCleanup >= settings.CleanupInterval)
                    {
                        await CleanupAsync(settings, stoppingToken);
                        lastCleanup = DateTimeOffset.UtcNow;
                    }  //max 1 ad ogni CleanupInterval (1 h) esegue la pulizia eliminando i mexs vecchi.

                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Outbox processing failed for {DbContext}", typeof(TContext).Name);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown normale
        }
    }


    
    private async Task<(int Fetched, int Failed)> ProcessBatchAsync(string selectSql, OutboxOptions settings, CancellationToken cancellationToken)
    {

        await using var scope = scopeFactory.CreateAsyncScope();  //await using lo chiude alla fine del batch
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        //1 scope nuovo x ogni batch


        var strategy = dbContext.Database.CreateExecutionStrategy();  //necessario se il DbContext usa EnableRetryOnFailure: le transazioni esplicite vanno nella execution strategy

        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();  //assicuri che riparta pulita
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);  //

            var messages = await dbContext.Set<OutboxMessage>()
                .FromSqlRaw(selectSql, settings.MaxAttempts, settings.BatchSize)
                .ToListAsync(cancellationToken);

            var failed = 0;
            foreach (var message in messages)
            {
                try
                {
                    await publisher.PublishRawAsync(new OutgoingMessage(
                        message.Id,
                        message.Exchange,
                        message.EventName,
                        Encoding.UTF8.GetBytes(message.Payload),
                        message.OccurredOnUtc,
                        message.TraceParent,
                        message.TraceState), cancellationToken);

                    message.MarkProcessed(DateTimeOffset.UtcNow);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    failed++;
                    message.MarkFailed(exception);
                    MessagingDiagnostics.OutboxPublishFailures.Add(1, new KeyValuePair<string, object?>("messaging.event.name", message.EventName));

                    if (message.Attempts >= settings.MaxAttempts)
                        logger.LogError(exception, "Outbox message {MessageId} ({EventName}) reached {MaxAttempts} attempts and will not be retried", message.Id, message.EventName, settings.MaxAttempts);
                    else
                        logger.LogWarning(exception, "Publishing outbox message {MessageId} ({EventName}) failed (attempt {Attempt})", message.Id, message.EventName, message.Attempts);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (messages.Count > 0)
                logger.LogDebug("Outbox: {Published} published, {Failed} failed", messages.Count - failed, failed);

            return (messages.Count, failed);
        });
    }

    private async Task CleanupAsync(OutboxOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        var cutoff = DateTimeOffset.UtcNow - settings.Retention;

        var deleted = await dbContext.Set<OutboxMessage>()
            .Where(m => m.ProcessedOnUtc != null && m.ProcessedOnUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            logger.LogInformation("Outbox cleanup: {Deleted} published messages older than {Retention} removed", deleted, settings.Retention);
    }


    private string BuildSelectSql()
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        var entityType = dbContext.Model.FindEntityType(typeof(OutboxMessage))
            ?? throw new InvalidOperationException(
                $"{typeof(TContext).Name} non mappa OutboxMessage: chiama modelBuilder.ApplyOutboxConfiguration() in OnModelCreating.");

        var tableName = entityType.GetTableName() ?? OutboxMessageConfiguration.TableName;
        var schema = entityType.GetSchema();
        var table = schema is null ? $"\"{tableName}\"" : $"\"{schema}\".\"{tableName}\"";

        // {0} = MaxAttempts, {1} = BatchSize → diventano parametri SQL, niente injection
        return $$"""
                SELECT * FROM {{table}}
                WHERE "{{OutboxMessageConfiguration.ProcessedOnUtcColumn}}" IS NULL
                  AND "{{OutboxMessageConfiguration.AttemptsColumn}}" < {0}
                ORDER BY "{{OutboxMessageConfiguration.OccurredOnUtcColumn}}"
                LIMIT {1}
                FOR UPDATE SKIP LOCKED
                """;
    }


}