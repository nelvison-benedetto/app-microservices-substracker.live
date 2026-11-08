using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Subscriptions.Core.Subscriptions;
using SubSnapMS.Subscriptions.Infrastructure.Persistence;

namespace SubSnapMS.Subscriptions.API.Backfill;

//"--republish-events": accoda nell'Outbox un SubscriptionCreated per ogni abbonamento esistente (stato e Version attuali) ed esce. Gli eventi partono al successivo avvio normale del servizio.
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
            //Scope nuovo per batch: DbContext leggero, Outbox svuotata a ogni SaveChanges
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SubscriptionsDbContext>();
            var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();

            var batch = await dbContext.Subscriptions
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .Skip(total)
                .Take(BatchSize)
                .ToListAsync();

            if (batch.Count == 0)
                break;

            foreach (var subscription in batch)
            {
                var details = subscription.Details;

                outbox.Add(new SubscriptionCreated
                {
                    SubscriptionId = subscription.Id.Value,
                    UserId = subscription.UserId.Value,
                    Version = subscription.Version,
                    Name = details.Name,
                    Category = details.Category,
                    Amount = details.Price.Amount,
                    Currency = details.Price.Currency,
                    BillingCycle = details.BillingCycle.ToCode(),
                    StartDate = details.Period.StartDate,
                    EndDate = details.Period.EndDate,
                    CreatedOnUtc = subscription.CreatedAtUtc,
                    OccurredOnUtc = now
                });
            }

            await dbContext.SaveChangesAsync();
            total += batch.Count;
            logger.LogInformation("Republish: {Total} subscriptions queued", total);
        }

        logger.LogInformation("Republish completed: {Total} SubscriptionCreated events in the outbox", total);
        return true;
    }
}