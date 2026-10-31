namespace SubSnapMS.Analytics.API.Features.GetDashboard;

/// <param name="Today">Data di riferimento (fuso Europe/Rome).</param>
/// <param name="ActiveSubscriptions">Abbonamenti attivi oggi.</param>
public sealed record DashboardResponse(
    DateOnly Today,
    int ActiveSubscriptions,
    IReadOnlyList<CurrencyTotalResponse> Totals,
    IReadOnlyList<CategoryBreakdownResponse> Categories,
    IReadOnlyList<UpcomingRenewalResponse> UpcomingRenewals,
    IReadOnlyList<TopSubscriptionResponse> MostExpensive);

/// <param name="YearlyTotal">MonthlyTotal × 12.</param>
public sealed record CurrencyTotalResponse(string Currency, decimal MonthlyTotal, decimal YearlyTotal, int ActiveCount);

/// <param name="Category">Null = senza categoria (il frontend mostra "Senza categoria").</param>
/// <param name="SharePercent">Quota del totale mensile della stessa valuta, 1 decimale.</param>
public sealed record CategoryBreakdownResponse(string Currency, string? Category, decimal MonthlyTotal, int Count, decimal SharePercent);

/// <param name="Amount">Importo addebitato (prezzo pieno, non il mensile).</param>
public sealed record UpcomingRenewalResponse(Guid SubscriptionId, string Name, DateOnly Date, decimal Amount, string Currency, string BillingCycle);

public sealed record TopSubscriptionResponse(Guid SubscriptionId, string Name, decimal MonthlyAmount, string Currency, string BillingCycle);