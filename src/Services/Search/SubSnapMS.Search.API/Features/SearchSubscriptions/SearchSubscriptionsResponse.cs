namespace SubSnapMS.Search.API.Features.SearchSubscriptions;

public sealed record SearchSubscriptionsResponse(
    long Total,  //risultati totali (con i filtri applicati)
    int Page,
    int PageSize,
    IReadOnlyList<SubscriptionHitResponse> Items,
    SearchFacetsResponse Facets);

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
    double? Score);  //rilevanza, null quando si ordina per un campo

//Conteggi per il testo cercato, indipendenti dai filtri selezionati.</summary>
public sealed record SearchFacetsResponse(
    IReadOnlyList<FacetBucketResponse> Categories,
    IReadOnlyList<FacetBucketResponse> BillingCycles,
    IReadOnlyList<FacetBucketResponse> Currencies);

public sealed record FacetBucketResponse(string Value, long Count);