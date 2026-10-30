using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.RabbitMq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Pubblica su RabbitMQ i messaggi dell'Outbox. Con più repliche del servizio,
/// FOR UPDATE SKIP LOCKED fa sì che ogni riga venga presa da una sola replica.
/// Garanzia: at-least-once (se il commit fallisce dopo la publish, il messaggio verrà ripubblicato).
/// </summary>
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
        var selectSql = BuildSelectSql();   // fallisce subito se il DbContext non mappa OutboxMessage

        logger.LogInformation("Outbox processor started for {DbContext} (every {Interval})", typeof(TContext).Name, settings.PollingInterval);

        using var timer = new PeriodicTimer(settings.PollingInterval);
        var lastCleanup = DateTimeOffset.MinValue;

        try
        {
            do
            {
                try
                {
                    // Se il batch era pieno e tutto è andato bene, ce n'è probabilmente altro: continua subito
                    (int Fetched, int Failed) result;
                    do
                    {
                        result = await ProcessBatchAsync(selectSql, settings, stoppingToken);
                    }
                    while (result.Fetched == settings.BatchSize && result.Failed == 0 && !stoppingToken.IsCancellationRequested);

                    if (DateTimeOffset.UtcNow - lastCleanup >= settings.CleanupInterval)
                    {
                        await CleanupAsync(settings, stoppingToken);
                        lastCleanup = DateTimeOffset.UtcNow;
                    }
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
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

        // Necessario se il DbContext usa EnableRetryOnFailure: le transazioni esplicite vanno nella execution strategy
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

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
        return $"""
                SELECT * FROM {table}
                WHERE {OutboxMessageConfiguration.ProcessedOnUtcColumn} IS NULL
                  AND {OutboxMessageConfiguration.AttemptsColumn} < {{0}}
                ORDER BY {OutboxMessageConfiguration.OccurredOnUtcColumn}
                LIMIT {{1}}
                FOR UPDATE SKIP LOCKED
                """;
    }
}