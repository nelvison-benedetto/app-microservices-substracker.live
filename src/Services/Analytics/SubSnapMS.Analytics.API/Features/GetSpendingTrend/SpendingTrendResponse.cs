namespace SubSnapMS.Analytics.API.Features.GetSpendingTrend;

public sealed record SpendingTrendResponse(string FromMonth, string ToMonth, IReadOnlyList<CurrencyTrendResponse> Series);
//FromMonth è il primo mese, ToMonth è il mese corrente, Series è una serie per valuta ogni serie con un punto per mese (anche a zero)

public sealed record CurrencyTrendResponse(string Currency, IReadOnlyList<TrendPointResponse> Points);

public sealed record TrendPointResponse(string Month, decimal MonthlyCost, decimal Charged);
//Charged sono addebiti che cadono nel mese