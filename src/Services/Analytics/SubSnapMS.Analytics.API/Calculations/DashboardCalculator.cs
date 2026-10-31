using SubSnapMS.Analytics.API.Features.GetDashboard;

namespace SubSnapMS.Analytics.API.Calculations;

/// <summary>Calcolo puro della dashboard: stessi input → stessa risposta. Nessun I/O.</summary>
public static class DashboardCalculator
{
    public const int MostExpensiveCount = 5;

    public static DashboardResponse Build(IReadOnlyList<AnalyticsSubscription> subscriptions, DateOnly today, int upcomingDays)
    {
        var active = subscriptions.Where(s => s.IsActiveOn(today)).ToList();

        // ── Totali per valuta ─────────────────────────────────────────────────────
        var totals = active
            .GroupBy(s => s.Currency)
            .Select(group =>
            {
                var monthly = group.Sum(s => s.MonthlyAmount);
                return new CurrencyTotalResponse(group.Key, monthly, monthly * 12, group.Count());
            })
            .OrderByDescending(t => t.MonthlyTotal)
            .ToList();

        var monthlyByCurrency = totals.ToDictionary(t => t.Currency, t => t.MonthlyTotal);

        // ── Categorie (per valuta; "Netflix" e " netflix " nella stessa categoria) ──
        var categories = active
            .GroupBy(s => s.Currency)
            .SelectMany(byCurrency => byCurrency
                .GroupBy(s => s.CategoryKey, StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var monthly = group.Sum(s => s.MonthlyAmount);
                    var currencyTotal = monthlyByCurrency[byCurrency.Key];

                    return new CategoryBreakdownResponse(
                        byCurrency.Key,
                        group.Key.Length == 0 ? null : group.First().CategoryKey,
                        monthly,
                        group.Count(),
                        currencyTotal == 0 ? 0 : decimal.Round(monthly / currencyTotal * 100, 1, MidpointRounding.AwayFromZero));
                }))
            .OrderBy(c => c.Currency)
            .ThenByDescending(c => c.MonthlyTotal)
            .ToList();

        // ── Prossimi rinnovi: il primo addebito di ogni abbonamento nella finestra ─
        var windowEnd = today.AddDays(upcomingDays);
        var upcoming = subscriptions
            .Select(s => (Subscription: s, Date: BillingSchedule.FirstChargeBetween(s.StartDate, s.BillingCycle, s.EndDate, today, windowEnd)))
            .Where(x => x.Date is not null)
            .Select(x => new UpcomingRenewalResponse(
                x.Subscription.Id,
                x.Subscription.Name,
                x.Date!.Value,
                x.Subscription.Amount,
                x.Subscription.Currency,
                x.Subscription.BillingCycle))
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        // ── I più costosi (costo mensile) ─────────────────────────────────────────
        var mostExpensive = active
            .OrderByDescending(s => s.MonthlyAmount)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MostExpensiveCount)
            .Select(s => new TopSubscriptionResponse(s.Id, s.Name, s.MonthlyAmount, s.Currency, s.BillingCycle))
            .ToList();

        return new DashboardResponse(today, active.Count, totals, categories, upcoming, mostExpensive);
    }
}