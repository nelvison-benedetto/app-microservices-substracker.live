using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Infrastructure.Persistence;

namespace SubSnapMS.Identity.API.Backfill;

//"--republish-events": accoda un UserRegistered per ogni utente attivo (email attuale, data di registrazione originale).
//RegisteredOnUtc vecchia → Notification salva l'email ma NON invia il benvenuto.
//Solo id, email e data: la proiezione non carica password né refresh token.
public static class RepublishEventsCommand
{
    public const string Argument = "--republish-events";
    private const int BatchSize = 500;

    public static async Task<bool> RunIfRequestedAsync(WebApplication app, string[] args)
    {
        if (!args.Contains(Argument, StringComparer.OrdinalIgnoreCase))
            return false;

        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(RepublishEventsCommand));
        var now = TimeProvider.System.GetUtcNow();
        var total = 0;

        while (true)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();

            var batch = await dbContext.Users
                .AsNoTracking()
                .Where(u => u.Status == UserStatus.Active)
                .OrderBy(u => u.Id)
                .Skip(total)
                .Take(BatchSize)
                .Select(u => new { u.Id, u.Email, u.CreatedAtUtc })
                .ToListAsync();

            if (batch.Count == 0)
                break;

            foreach (var user in batch)
            {
                outbox.Add(new UserRegistered
                {
                    UserId = user.Id.Value,
                    Email = user.Email.Value,
                    RegisteredOnUtc = user.CreatedAtUtc,
                    OccurredOnUtc = now
                });
            }

            await dbContext.SaveChangesAsync();
            total += batch.Count;
            logger.LogInformation("Republish: {Total} users queued", total);
        }

        logger.LogInformation("Republish completed: {Total} UserRegistered events in the outbox", total);
        return true;
    }
}