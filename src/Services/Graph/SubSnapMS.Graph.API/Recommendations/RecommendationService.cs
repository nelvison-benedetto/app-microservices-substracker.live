using Microsoft.Extensions.Options;
using Neo4j.Driver;
using SubSnapMS.Graph.API.Configuration;
using SubSnapMS.Graph.API.Features.GetOverlaps;
using SubSnapMS.Graph.API.Features.GetServiceRecommendations;
using SubSnapMS.Graph.API.Neo4j;
using System.Runtime.Intrinsics.Arm;
using static SubSnapMS.Graph.API.Recommendations.CypherFragments;

namespace SubSnapMS.Graph.API.Recommendations;

public sealed class RecommendationService(
    GraphClient graph,
    GraphSchemaState state,
    GraphCalendar calendar,
    IOptions<GraphOptions> options)
{
    public const string BasedOnSimilarUsers = "similar-users";
    public const string BasedOnPopular = "popular";

    private const int MaxCategorySamples = 50;

    /// <summary>
    /// Collaborative filtering: utenti che hanno almeno uno dei miei servizi attivi → i loro altri servizi
    /// che io non ho. Score = somma delle sovrapposizioni (chi ha più servizi in comune con me pesa di più).
    /// Solo servizi con almeno $minUsers utenti distinti (privacy).
    /// </summary>
    private static readonly string SimilarUsersQuery = $"""
        MATCH (me:User {{id: $userId}})-[:HAS]->(ms:Subscription)-[:OF]->(mine:Service)
        WHERE {Active("ms")}
        WITH me, collect(DISTINCT mine) AS myServices
        UNWIND myServices AS shared
        MATCH (shared)<-[:OF]-(os:Subscription)<-[:HAS]-(other:User)
        WHERE other <> me AND {Active("os")}
        WITH me, myServices, other, count(DISTINCT shared) AS overlap
        MATCH (other)-[:HAS]->(rs:Subscription)-[:OF]->(rec:Service)
        WHERE {Active("rs")} AND NOT rec IN myServices
        WITH rec, count(DISTINCT other) AS users, sum(overlap) AS score,
             [c IN collect(rs.category) WHERE c IS NOT NULL][..{MaxCategorySamples}] AS categories
        WHERE users >= $minUsers
        RETURN rec.key AS key, rec.name AS name, users, score, categories
        ORDER BY score DESC, users DESC, name
        LIMIT $limit
        """;

    /// <summary>Fallback (utente nuovo o senza sovrapposizioni): i servizi più diffusi che non ha.</summary>
    private static readonly string PopularQuery = $"""
        MATCH (svc:Service)<-[:OF]-(s:Subscription)<-[:HAS]-(u:User)
        WHERE {Active("s")}
          AND NOT EXISTS {{MATCH(:User {{id: $userId}}) - [:HAS]->(mine: Subscription) - [:OF]->(svc)
            WHERE {Active("mine")}
          }}
        WITH svc, count(DISTINCT u) AS users,
             [c IN collect(s.category) WHERE c IS NOT NULL][..{MaxCategorySamples}] AS categories
        WHERE users >= $minUsers
        RETURN svc.key AS key, svc.name AS name, users, users AS score, categories
        ORDER BY users DESC, name
        LIMIT $limit
        """;

    private static readonly string MyActiveSubscriptionsQuery = $"""
        MATCH (:User {{id: $userId}})-[:HAS]->(s:Subscription)
        WHERE {Active("s")}
        RETURN s.id AS id, s.name AS name, s.category AS category, s.serviceKey AS serviceKey,
               s.monthlyAmount AS monthlyAmount, s.currency AS currency
        """;

    public async Task<ServiceRecommendationsResponse> GetServiceRecommendationsAsync(Guid userId, int limit, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        var parameters = new
        {
            userId = userId.ToString(),
            today = calendar.Today(),
            minUsers = (long)options.Value.MinUsersForRecommendation,
            limit = (long)limit
        };

        var similar = await graph.ReadAsync("recommend-similar-users", SimilarUsersQuery, parameters, cancellationToken);
        if (similar.Count > 0)
            return new ServiceRecommendationsResponse(BasedOnSimilarUsers, similar.Select(ToRecommendation).ToList());

        var popular = await graph.ReadAsync("recommend-popular", PopularQuery, parameters, cancellationToken);
        return new ServiceRecommendationsResponse(BasedOnPopular, popular.Select(ToRecommendation).ToList());
    }

    /// <summary>
    /// Sovrapposizioni tra i MIEI abbonamenti attivi:
    ///  - stessa categoria 2+ volte (es. tre servizi di streaming → valuta un bundle);
    ///  - stesso servizio 2+ volte (probabile doppione o abbonamento dimenticato).
    /// </summary>
    public async Task<OverlapsResponse> GetOverlapsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await state.WaitUntilReadyAsync(cancellationToken);

        var records = await graph.ReadAsync("my-overlaps", MyActiveSubscriptionsQuery, new
        {
            userId = userId.ToString(),
            today = calendar.Today()
        }, cancellationToken);

        var subscriptions = records.Select(record => new OwnSubscription(
            Guid.Parse(record["id"].As<string>()),
            record["name"].As<string>(),
            record["category"].As<string?>(),
            record["serviceKey"].As<string>(),
            decimal.Round((decimal)record["monthlyAmount"].As<double>(), 2, MidpointRounding.AwayFromZero),
            record["currency"].As<string>()))
            .ToList();

        var categories = subscriptions
            .Where(s => !string.IsNullOrWhiteSpace(s.Category))
            .GroupBy(s => s.Category!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() >= 2)
            .Select(group => new CategoryOverlapResponse(
                group.First().Category!.Trim(),
                Totals(group),
                group.OrderByDescending(s => s.MonthlyAmount).Select(ToItem).ToList()))
            .OrderByDescending(overlap => overlap.Subscriptions.Count)
            .ThenBy(overlap => overlap.Category, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var duplicates = subscriptions
            .GroupBy(s => s.ServiceKey)
            .Where(group => group.Count() >= 2)
            .Select(group => new DuplicateServiceResponse(
                group.First().Name,
                Totals(group),
                group.Select(ToItem).ToList()))
            .OrderBy(duplicate => duplicate.ServiceName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new OverlapsResponse(categories, duplicates);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────────

    private static ServiceRecommendationResponse ToRecommendation(IRecord record) => new(
        record["key"].As<string>(),
        record["name"].As<string>(),
        MostCommon(record["categories"].As<List<string>>()),
        record["users"].As<long>(),
        record["score"].As<long>());

    /// <summary>Categoria più usata dagli utenti per quel servizio (senza distinzione di maiuscole).</summary>
    private static string? MostCommon(IReadOnlyCollection<string> categories) =>
        categories
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .GroupBy(c => c.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First().Trim())
            .FirstOrDefault();

    private static IReadOnlyList<CurrencyTotalResponse> Totals(IEnumerable<OwnSubscription> subscriptions) =>
        subscriptions
            .GroupBy(s => s.Currency)
            .Select(group => new CurrencyTotalResponse(group.Key, group.Sum(s => s.MonthlyAmount)))
            .OrderByDescending(total => total.MonthlyTotal)
            .ToList();

    private static OverlapSubscriptionResponse ToItem(OwnSubscription subscription) => new(
        subscription.Id, subscription.Name, subscription.MonthlyAmount, subscription.Currency);

    private sealed record OwnSubscription(Guid Id, string Name, string? Category, string ServiceKey, decimal MonthlyAmount, string Currency);
}