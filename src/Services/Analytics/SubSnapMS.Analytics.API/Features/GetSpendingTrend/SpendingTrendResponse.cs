namespace SubSnapMS.Analytics.API.Features.GetSpendingTrend;

/// <param name="FromMonth">Primo mese, "yyyy-MM".</param>
/// <param name="ToMonth">Mese corrente, "yyyy-MM".</param>
/// <param name="Series">Una serie per valuta, ogni serie con un punto per mese (anche a zero).</param>
public sealed record SpendingTrendResponse(string FromMonth, string ToMonth, IReadOnlyList<CurrencyTrendResponse> Series);

public sealed record CurrencyTrendResponse(string Currency, IReadOnlyList<TrendPointResponse> Points);

/// <param name="Month">"yyyy-MM".</param>
/// <param name="MonthlyCost">Costo mensile degli abbonamenti attivi nel mese.</param>
/// <param name="Charged">Addebiti che cadono nel mese.</param>
public sealed record TrendPointResponse(string Month, decimal MonthlyCost, decimal Charged);