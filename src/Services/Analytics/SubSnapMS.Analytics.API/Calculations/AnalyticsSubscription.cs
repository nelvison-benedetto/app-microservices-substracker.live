namespace SubSnapMS.Analytics.API.Calculations;

//Abbonamento per i calcoli: indipendente da Mongo, quindi i calcoli si testano senza database
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
    //Stessa regola e stesso arrotondamento di Money.ToMonthly in Subscriptions
    public decimal MonthlyAmount { get; } = MonthlyCost.Calculate(Amount, BillingCycle);

    //Chiave di raggruppamento: categoria senza spazi, "" se assente
    public string CategoryKey => Category?.Trim() ?? string.Empty;

    //Attivo in una data (inizio e fine incluse), come SubscriptionPeriod.IsActiveOn
    public bool IsActiveOn(DateOnly date) => StartDate <= date && (EndDate is null || EndDate.Value >= date);

    //Attivo in almeno un giorno dell'intervallo
    public bool IsActiveDuring(DateOnly from, DateOnly to) => StartDate <= to && (EndDate is null || EndDate.Value >= from);
}