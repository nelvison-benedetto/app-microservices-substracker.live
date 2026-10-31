namespace SubSnapMS.Graph.API.Features.GetOverlaps;

/// <param name="Categories">Categorie con 2+ abbonamenti attivi (es. tre servizi di streaming).</param>
/// <param name="Duplicates">Stesso servizio attivo più volte (probabile doppione).</param>
public sealed record OverlapsResponse(
    IReadOnlyList<CategoryOverlapResponse> Categories,
    IReadOnlyList<DuplicateServiceResponse> Duplicates);

public sealed record CategoryOverlapResponse(
    string Category,
    IReadOnlyList<CurrencyTotalResponse> MonthlyTotals,
    IReadOnlyList<OverlapSubscriptionResponse> Subscriptions);

public sealed record DuplicateServiceResponse(
    string ServiceName,
    IReadOnlyList<CurrencyTotalResponse> MonthlyTotals,
    IReadOnlyList<OverlapSubscriptionResponse> Subscriptions);

public sealed record CurrencyTotalResponse(string Currency, decimal MonthlyTotal);

public sealed record OverlapSubscriptionResponse(Guid SubscriptionId, string Name, decimal MonthlyAmount, string Currency);