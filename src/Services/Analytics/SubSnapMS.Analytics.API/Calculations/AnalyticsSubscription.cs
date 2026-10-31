namespace SubSnapMS.Analytics.API.Calculations;

/// <summary>Abbonamento per i calcoli: indipendente da Mongo, quindi i calcoli si testano senza database.</summary>
public sealed record AnalyticsSubscription(
    Guid Id,
    string Name,
    string? Category,
    decimal Amount,
    string Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate)
{
    /// <summary>Stessa regola e stesso arrotondamento di Money.ToMonthly in Subscriptions.</summary>
    public decimal MonthlyAmount { get; } = MonthlyCost.Calculate(Amount, BillingCycle);

    /// <summary>Chiave di raggruppamento: categoria senza spazi, "" se assente.</summary>
    public string CategoryKey => Category?.Trim() ?? string.Empty;

    /// <summary>Attivo in una data (inizio e fine incluse), come SubscriptionPeriod.IsActiveOn.</summary>
    public bool IsActiveOn(DateOnly date) => StartDate <= date && (EndDate is null || EndDate.Value >= date);

    /// <summary>Attivo in almeno un giorno dell'intervallo.</summary>
    public bool IsActiveDuring(DateOnly from, DateOnly to) => StartDate <= to && (EndDate is null || EndDate.Value >= from);
}