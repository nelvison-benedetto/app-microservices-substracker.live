using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubSnapMS.Identity.Core.Deletions;
using SubSnapMS.Identity.Infrastructure.Persistence;

namespace SubSnapMS.Identity.Infrastructure.Background;

//Segnala le saghe di cancellazione bloccate (un servizio non ha confermato entro la soglia): log Warning con i servizi mancanti → Sentry/alert. Di solito il messaggio è nella DLQ di quel servizio: si corregge il problema e lo si riprocessa. Non forza la cancellazione: i dati di quel servizio esistono ancora.
internal sealed class UserDeletionMonitor(
    IServiceScopeFactory scopeFactory,
    IOptions<UserDeletionMonitorOptions> options,
    TimeProvider timeProvider,
    ILogger<UserDeletionMonitor> logger) : BackgroundService
{
    private const int MaxReportedPerCheck = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CheckInterval, timeProvider);

        try
        {
            do
            {
                try
                {
                    await CheckAsync(stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "User deletion monitor check failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            //shutdown normale
        }
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var cutoff = timeProvider.GetUtcNow() - options.Value.StuckAfter;

        var stuck = await dbContext.UserDeletions
            .AsNoTracking()
            .Where(d => d.Status == UserDeletionStatus.InProgress && d.RequestedAtUtc < cutoff)
            .OrderBy(d => d.RequestedAtUtc)
            .Take(MaxReportedPerCheck)
            .ToListAsync(cancellationToken);

        foreach (var deletion in stuck)
        {
            logger.LogWarning(
                "User deletion {UserId} stuck since {RequestedAt}: still waiting for {PendingServices}",
                deletion.Id, deletion.RequestedAtUtc, string.Join(", ", deletion.PendingServices));
        }
    }
}