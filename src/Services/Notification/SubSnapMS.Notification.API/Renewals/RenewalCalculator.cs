using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Notification.API.Renewals;

/// <summary>
/// Prossimo rinnovo (≥ oggi) di un abbonamento. Ogni data si calcola dalla StartDate e mai dal rinnovo precedente,
/// quindi non c'è deriva: 31 gen → 28 feb → 31 mar (e non 28 mar).
/// </summary>
public static class RenewalCalculator
{
    /// <returns>Null se l'abbonamento termina prima del prossimo rinnovo o il ciclo è sconosciuto.</returns>
    public static DateOnly? NextRenewal(DateOnly startDate, string billingCycle, DateOnly? endDate, DateOnly today)
    {
        DateOnly? next = billingCycle switch
        {
            BillingCycles.Weekly => NextByDays(startDate, 7, today),
            BillingCycles.Monthly => NextByMonths(startDate, 1, today),
            BillingCycles.Quarterly => NextByMonths(startDate, 3, today),
            BillingCycles.Biannual => NextByMonths(startDate, 6, today),
            BillingCycles.Yearly => NextByMonths(startDate, 12, today),
            _ => null
        };

        return next is { } date && endDate is { } end && date > end ? null : next;
    }

    private static DateOnly NextByDays(DateOnly start, int periodDays, DateOnly today)
    {
        if (start >= today)
            return start;

        var elapsedDays = today.DayNumber - start.DayNumber;
        var periods = (elapsedDays + periodDays - 1) / periodDays;   // arrotondato per eccesso
        return start.AddDays(periods * periodDays);
    }

    private static DateOnly NextByMonths(DateOnly start, int periodMonths, DateOnly today)
    {
        if (start >= today)
            return start;

        var elapsedMonths = (today.Year - start.Year) * 12 + today.Month - start.Month;
        var periods = elapsedMonths / periodMonths;
        var candidate = start.AddMonths(periods * periodMonths);

        while (candidate < today)
        {
            periods++;
            candidate = start.AddMonths(periods * periodMonths);
        }

        return candidate;
    }
}