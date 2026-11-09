namespace SubSnapMS.Analytics.API.Features.GetDashboard;

public sealed record DashboardResponse(
    DateOnly Today,  //Data di riferimento (fuso Europe/Rome)
    int ActiveSubscriptions,  //Abbonamenti attivi oggi
    IReadOnlyList<CurrencyTotalResponse> Totals,
    IReadOnlyList<CategoryBreakdownResponse> Categories,
    IReadOnlyList<UpcomingRenewalResponse> UpcomingRenewals,
    IReadOnlyList<TopSubscriptionResponse> MostExpensive);

public sealed record CurrencyTotalResponse(string Currency, decimal MonthlyTotal, decimal YearlyTotal, int ActiveCount);

public sealed record CategoryBreakdownResponse(string Currency, string? Category, decimal MonthlyTotal, int Count, decimal SharePercent);
//Category se è null il frontend mostra 'senza categoria', SharePercent è quota del totale mensile della stessa valuta, 1 decimale.

public sealed record UpcomingRenewalResponse(Guid SubscriptionId, string Name, DateOnly Date, decimal Amount, string Currency, string BillingCycle);
//Amount è l' importo addebitato (prezzo pieno, non il mensile)

public sealed record TopSubscriptionResponse(Guid SubscriptionId, string Name, decimal MonthlyAmount, string Currency, string BillingCycle);