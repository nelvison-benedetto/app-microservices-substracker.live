namespace SubSnapMS.Graph.API.Features.GetOverlaps;

public sealed record OverlapsResponse(
    IReadOnlyList<CategoryOverlapResponse> Categories,  //categorie con 2+ abbonamenti attivi (es. tre servizi di streaming)
    IReadOnlyList<DuplicateServiceResponse> Duplicates);  //stesso servizio attivo più volte (probabile doppione)

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