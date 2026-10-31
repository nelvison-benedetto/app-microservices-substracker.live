using Microsoft.EntityFrameworkCore;
using SubSnapMS.Subscriptions.Application.Abstractions.Queries;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;
using SubSnapMS.Subscriptions.Infrastructure.Persistence.Configurations;

namespace SubSnapMS.Subscriptions.Infrastructure.Persistence.Queries;

/// <summary>
/// Lato lettura: AsNoTracking. Si materializza il Value Object e si mappa con SubscriptionResult.From,
/// così il costo mensile viene calcolato dalla stessa regola del dominio (Money.ToMonthly), non duplicato in SQL.
/// </summary>
internal sealed class SubscriptionQueries(SubscriptionsDbContext dbContext) : ISubscriptionQueries
{
    public async Task<IReadOnlyList<SubscriptionResult>> GetUserSubscriptionsAsync(UserId userId, CancellationToken cancellationToken)
    {
        var subscriptions = await dbContext.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return subscriptions.Select(SubscriptionResult.From).ToList();
    }

    public async Task<SubscriptionDetailResult?> GetDetailAsync(UserId userId, SubscriptionId subscriptionId, CancellationToken cancellationToken)
    {
        // Filtro per id E utente: un abbonamento altrui non viene trovato (→ 404)
        var subscription = await dbContext.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId, cancellationToken);

        if (subscription is null)
            return null;

        var history = await dbContext.SubscriptionHistory
            .AsNoTracking()
            .Where(e => EF.Property<SubscriptionId>(e, SubscriptionHistoryEntryConfiguration.SubscriptionIdProperty) == subscriptionId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);

        return new SubscriptionDetailResult(
            SubscriptionResult.From(subscription),
            history.Select(SubscriptionHistoryEntryResult.From).ToList());
    }
}