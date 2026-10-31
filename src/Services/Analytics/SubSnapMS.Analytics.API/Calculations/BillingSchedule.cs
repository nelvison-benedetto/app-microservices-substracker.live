using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Analytics.API.Calculations;

/// <summary>
/// Date di addebito di un abbonamento: StartDate, poi ogni periodo. Ogni data si calcola dalla StartDate
/// (31 gen → 28 feb → 31 mar, senza deriva). Nessun addebito dopo EndDate.
/// </summary>
public static class BillingSchedule
{
    /// <summary>Addebiti tra from e to (inclusi).</summary>
    public static IEnumerable<DateOnly> ChargeDates(DateOnly startDate, string billingCycle, DateOnly? endDate, DateOnly from, DateOnly to)
    {
        var first = from < startDate ? startDate : from;
        var last = endDate is { } end && end < to ? end : to;

        if (last < first)
            return [];

        return billingCycle switch
        {
            BillingCycles.Weekly => ByDays(startDate, 7, first, last),
            BillingCycles.Monthly => ByMonths(startDate, 1, first, last),
            BillingCycles.Quarterly => ByMonths(startDate, 3, first, last),
            BillingCycles.Biannual => ByMonths(startDate, 6, first, last),
            BillingCycles.Yearly => ByMonths(startDate, 12, first, last),
            _ => Enumerable.Empty<DateOnly>()
        };
    }

    public static DateOnly? FirstChargeBetween(DateOnly startDate, string billingCycle, DateOnly? endDate, DateOnly from, DateOnly to)
    {
        foreach (var date in ChargeDates(startDate, billingCycle, endDate, from, to))
            return date;

        return null;
    }

    private static IEnumerable<DateOnly> ByDays(DateOnly start, int periodDays, DateOnly first, DateOnly last)
    {
        var periods = (first.DayNumber - start.DayNumber + periodDays - 1) / periodDays;   // primo addebito ≥ first

        for (var date = start.AddDays(periods * periodDays); date <= last; date = date.AddDays(periodDays))
            yield return date;
    }

    private static IEnumerable<DateOnly> ByMonths(DateOnly start, int periodMonths, DateOnly first, DateOnly last)
    {
        var elapsedMonths = (first.Year - start.Year) * 12 + first.Month - start.Month;
        var periods = Math.Max(0, elapsedMonths / periodMonths - 1);   // un periodo prima: copre i fine mese accorciati
        var date = start.AddMonths(periods * periodMonths);

        while (date < first)
        {
            periods++;
            date = start.AddMonths(periods * periodMonths);
        }

        while (date <= last)
        {
            yield return date;
            periods++;
            date = start.AddMonths(periods * periodMonths);
        }
    }
}