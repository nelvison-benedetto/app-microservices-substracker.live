using System.Text.Json;
using SubSnapMS.Search.API.Elasticsearch;
using SubSnapMS.Search.API.Features.SearchSubscriptions;
using SubSnapMS.Search.API.Features.Suggest;
using SubSnapMS.Search.API.Indexing;

namespace SubSnapMS.Search.API.Querying;

/// <summary>Esegue le ricerche e traduce la risposta di Elasticsearch nei DTO dell'API.</summary>
public sealed class SubscriptionSearchService(ElasticHttpClient elastic, SearchIndexState state, SearchCalendar calendar)
{
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(5);

    public async Task<SearchSubscriptionsResponse> SearchAsync(Guid userId, SubscriptionSearchCriteria criteria, CancellationToken cancellationToken)
    {
        await WaitForIndicesAsync(cancellationToken);

        var body = SubscriptionQueryBuilder.BuildSearch(userId, criteria, calendar.Today());
        using var result = await elastic.SearchAsync(SearchIndices.Subscriptions, body, cancellationToken);

        var root = result.RootElement;
        var hits = root.GetProperty("hits");

        var items = hits.GetProperty("hits").EnumerateArray()
            .Select(hit =>
            {
                var source = hit.GetProperty("_source").Deserialize<SubscriptionSearchDocument>(ElasticHttpClient.JsonOptions)!;
                double? score = hit.TryGetProperty("_score", out var scoreElement) && scoreElement.ValueKind == JsonValueKind.Number
                    ? scoreElement.GetDouble()
                    : null;

                return new SubscriptionHitResponse(
                    source.SubscriptionId,
                    source.Name,
                    source.Category,
                    source.Amount,
                    source.MonthlyAmount,
                    source.Currency,
                    source.BillingCycle,
                    source.StartDate,
                    source.EndDate,
                    score);
            })
            .ToList();

        var aggregations = root.GetProperty("aggregations");

        return new SearchSubscriptionsResponse(
            hits.GetProperty("total").GetProperty("value").GetInt64(),
            criteria.Page,
            criteria.PageSize,
            items,
            new SearchFacetsResponse(
                Buckets(aggregations, "categories"),
                Buckets(aggregations, "billingCycles"),
                Buckets(aggregations, "currencies")));
    }

    public async Task<SuggestResponse> SuggestAsync(Guid userId, string prefix, int limit, CancellationToken cancellationToken)
    {
        await WaitForIndicesAsync(cancellationToken);

        using var result = await elastic.SearchAsync(
            SearchIndices.Subscriptions, SubscriptionQueryBuilder.BuildSuggest(userId, prefix, limit), cancellationToken);

        var suggestions = result.RootElement.GetProperty("hits").GetProperty("hits").EnumerateArray()
            .Select(hit =>
            {
                var source = hit.GetProperty("_source");
                return new SuggestionResponse(
                    source.GetProperty("subscriptionId").GetGuid(),
                    source.GetProperty("name").GetString()!);
            })
            .ToList();

        return new SuggestResponse(suggestions);
    }

    private async Task WaitForIndicesAsync(CancellationToken cancellationToken)
    {
        if (state.IsReady)
            return;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ReadyTimeout);
        await state.WaitUntilReadyAsync(timeout.Token);
    }

    private static IReadOnlyList<FacetBucketResponse> Buckets(JsonElement aggregations, string name) =>
        aggregations.GetProperty(name).GetProperty("buckets").EnumerateArray()
            .Select(bucket => new FacetBucketResponse(
                bucket.GetProperty("key").GetString()!,
                bucket.GetProperty("doc_count").GetInt64()))
            .ToList();
}