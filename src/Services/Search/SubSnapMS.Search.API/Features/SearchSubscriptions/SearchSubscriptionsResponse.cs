namespace SubSnapMS.Search.API.Features.SearchSubscriptions;

/// <param name="Total">Risultati totali (con i filtri applicati).</param>
public sealed record SearchSubscriptionsResponse(
    long Total,
    int Page,
    int PageSize,
    IReadOnlyList<SubscriptionHitResponse> Items,
    SearchFacetsResponse Facets);

/// <param name="Score">Rilevanza; null quando si ordina per un campo.</param>
public sealed record SubscriptionHitResponse(
    Guid SubscriptionId,
    string Name,
    string? Category,
    decimal Amount,
    decimal MonthlyAmount,
    string Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate,
    double? Score);

/// <summary>Conteggi per il testo cercato, indipendenti dai filtri selezionati.</summary>
public sealed record SearchFacetsResponse(
    IReadOnlyList<FacetBucketResponse> Categories,
    IReadOnlyList<FacetBucketResponse> BillingCycles,
    IReadOnlyList<FacetBucketResponse> Currencies);

public sealed record FacetBucketResponse(string Value, long Count);