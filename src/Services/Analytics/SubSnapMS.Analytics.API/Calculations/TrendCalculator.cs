using System.Globalization;
using SubSnapMS.Analytics.API.Features.GetSpendingTrend;

namespace SubSnapMS.Analytics.API.Calculations;

/// <summary>
/// Andamento mese per mese, una serie per valuta. Per ogni mese:
///  - MonthlyCost: costo mensile degli abbonamenti attivi in almeno un giorno del mese;
///  - Charged: addebiti che cadono nel mese (per il mese corrente include quelli previsti fino a fine mese).
/// </summary>
public static class TrendCalculator
{
    public static SpendingTrendResponse Build(IReadOnlyList<AnalyticsSubscription> subscriptions, DateOnly today, int months)
    {
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var firstMonth = currentMonth.AddMonths(-(months - 1));
        var windowEnd = currentMonth.AddMonths(1).AddDays(-1);

        var series = subscriptions
            .Where(s => s.IsActiveDuring(firstMonth, windowEnd))
            .GroupBy(s => s.Currency)
            .OrderBy(group => group.Key)
            .Select(group => new CurrencyTrendResponse(
                group.Key,
                Enumerable.Range(0, months)
                    .Select(offset => BuildPoint(group.ToList(), firstMonth.AddMonths(offset)))
                    .ToList()))
            .ToList();

        return new SpendingTrendResponse(FormatMonth(firstMonth), FormatMonth(currentMonth), series);
    }

    private static TrendPointResponse BuildPoint(IReadOnlyList<AnalyticsSubscription> subscriptions, DateOnly monthStart)
    {
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var monthlyCost = subscriptions
            .Where(s => s.IsActiveDuring(monthStart, monthEnd))
            .Sum(s => s.MonthlyAmount);

        var charged = subscriptions
            .Sum(s => s.Amount * BillingSchedule.ChargeDates(s.StartDate, s.BillingCycle, s.EndDate, monthStart, monthEnd).Count());

        return new TrendPointResponse(FormatMonth(monthStart), monthlyCost, charged);
    }

    private static string FormatMonth(DateOnly month) => month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
}