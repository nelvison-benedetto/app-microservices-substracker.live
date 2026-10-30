using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

/// <summary>Cancella periodicamente i record Inbox più vecchi della retention.</summary>
internal sealed class InboxCleanupService<TContext>(
    IServiceScopeFactory scopeFactory,
    IOptions<InboxOptions> options,
    ILogger<InboxCleanupService<TContext>> logger) : BackgroundService
    where TContext : DbContext
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        using var timer = new PeriodicTimer(settings.CleanupInterval);

        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
                    var cutoff = DateTimeOffset.UtcNow - settings.Retention;

                    var deleted = await dbContext.Set<InboxMessage>()
                        .Where(m => m.ProcessedOnUtc < cutoff)
                        .ExecuteDeleteAsync(stoppingToken);

                    if (deleted > 0)
                        logger.LogInformation("Inbox cleanup: {Deleted} records older than {Retention} removed", deleted, settings.Retention);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Inbox cleanup failed for {DbContext}", typeof(TContext).Name);
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown normale
        }
    }
}