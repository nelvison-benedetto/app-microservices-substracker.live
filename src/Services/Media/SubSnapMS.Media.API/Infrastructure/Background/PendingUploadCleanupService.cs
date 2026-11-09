using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.API.Infrastructure.Storage;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Infrastructure.Background;

//Elimina le richieste di upload mai confermate (riga Pending + eventuale oggetto su R2).
//Nessun evento: per il resto della piattaforma questi file non sono mai esistiti.
internal sealed class PendingUploadCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<MediaUploadOptions> options,
    TimeProvider timeProvider,
    ILogger<PendingUploadCleanupService> logger) : BackgroundService
{
    private const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.CleanupInterval, timeProvider);

        try
        {
            do
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Pending upload cleanup failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown normale
        }
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IObjectStorage>();

        var cutoff = timeProvider.GetUtcNow() - options.Value.PendingCleanupAfter;

        var abandoned = await dbContext.MediaFiles
            .Where(f => f.Status == MediaFileStatus.Pending && f.UploadExpiresAtUtc < cutoff)
            .OrderBy(f => f.UploadExpiresAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (abandoned.Count == 0)
            return;

        foreach (var file in abandoned)
        {
            await storage.DeleteAsync(file.ObjectKey, cancellationToken);   //può non esistere: va bene
            dbContext.MediaFiles.Remove(file);                              //nessun Delete() di dominio: nessun evento
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Removed {Count} abandoned uploads", abandoned.Count);
    }
}