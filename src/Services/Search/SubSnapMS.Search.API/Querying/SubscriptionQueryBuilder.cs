using System.Globalization;
using System.Text.Json.Nodes;
using SubSnapMS.Search.API.Elasticsearch;

namespace SubSnapMS.Search.API.Querying;

//Costruisce il body di _search. Struttura:
// query       → utente + non cancellati (filter, sempre) + testo (punteggio)
// post_filter → filtri scelti dall'utente (non influenzano i conteggi delle faccette)
// aggs        → conteggi per categoria, ciclo, valuta
public static class SubscriptionQueryBuilder
{
    public const int FacetSize = 50;

    public static JsonObject BuildSearch(Guid userId, SubscriptionSearchCriteria criteria, DateOnly today)
    {
        var body = new JsonObject
        {
            ["query"] = BaseQuery(userId, criteria.Text),
            ["from"] = criteria.From,
            ["size"] = criteria.PageSize,
            ["track_total_hits"] = true,
            ["sort"] = Sort(criteria.Sort, criteria.Text is not null),
            ["aggs"] = new JsonObject
            {
                ["categories"] = Terms("category.raw", FacetSize),
                ["billingCycles"] = Terms("billingCycle", 10),
                ["currencies"] = Terms("currency", 20)
            }
        };

        var postFilters = PostFilters(criteria, today);
        if (postFilters.Count > 0)
            body["post_filter"] = EsQuery.Bool(filter: postFilters);

        return body;
    }

    //Autocomplete: prefissi sul nome, un risultato per nome (collapse), solo i campi necessari
    public static JsonObject BuildSuggest(Guid userId, string prefix, int limit) => new()
    {
        ["query"] = EsQuery.Bool(
            filter: OwnerFilters(userId),
            must: new JsonArray(new JsonObject
            {
                ["match"] = new JsonObject
                {
                    ["name.autocomplete"] = new JsonObject { ["query"] = prefix, ["operator"] = "and" }
                }
            })),
        ["size"] = limit,
        ["collapse"] = new JsonObject { ["field"] = "name.sort" },
        ["sort"] = new JsonArray("_score", new JsonObject { ["name.sort"] = "asc" }),
        ["_source"] = new JsonArray("subscriptionId", "name")
    };

    // Parti della query 

    //Sicurezza: l'utente arriva dal JWT. Questo filtro c'è SEMPRE
    private static JsonArray OwnerFilters(Guid userId) => new(
        EsQuery.Term("userId", userId.ToString()),
        EsQuery.Term("deleted", false));

    private static JsonObject BaseQuery(Guid userId, string? text) =>
        text is null
            ? EsQuery.Bool(filter: OwnerFilters(userId))
            : EsQuery.Bool(filter: OwnerFilters(userId), must: new JsonArray(TextQuery(text)));

    //Due modi di trovare un risultato (basta uno):
    // - fuzzy su nome (peso 3) e categoria: tollera errori ("netflx" → Netflix); prefix_length 1 = prima lettera esatta;
    // - prefisso sul nome via edge n-gram: "net" trova Netflix mentre l'utente scrive (peso 2).
    private static JsonObject TextQuery(string text) => EsQuery.Bool(
        should: new JsonArray(
            new JsonObject
            {
                ["multi_match"] = new JsonObject
                {
                    ["query"] = text,
                    ["fields"] = new JsonArray("name^3", "category"),
                    ["fuzziness"] = "AUTO",
                    ["prefix_length"] = 1
                }
            },
            new JsonObject
            {
                ["match"] = new JsonObject
                {
                    ["name.autocomplete"] = new JsonObject { ["query"] = text, ["operator"] = "and", ["boost"] = 2 }
                }
            }),
        minimumShouldMatch: 1);

    private static JsonArray PostFilters(SubscriptionSearchCriteria criteria, DateOnly today)
    {
        var filters = new JsonArray();
        var todayText = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        if (criteria.Category is not null)
            filters.Add(EsQuery.Term("category.keyword", criteria.Category));   // il normalizer rende il confronto case-insensitive

        if (criteria.BillingCycle is not null)
            filters.Add(EsQuery.Term("billingCycle", criteria.BillingCycle));

        if (criteria.Currency is not null)
            filters.Add(EsQuery.Term("currency", criteria.Currency));

        switch (criteria.Status)
        {
            case SubscriptionStatusFilter.Active:
                // iniziato e (senza fine oppure fine ≥ oggi)
                filters.Add(EsQuery.Bool(
                    filter: new JsonArray(EsQuery.Range("startDate", "lte", todayText)),
                    should: new JsonArray(
                        EsQuery.Bool(mustNot: new JsonArray(EsQuery.Exists("endDate"))),
                        EsQuery.Range("endDate", "gte", todayText)),
                    minimumShouldMatch: 1));
                break;

            case SubscriptionStatusFilter.Ended:
                filters.Add(EsQuery.Range("endDate", "lt", todayText));
                break;
        }

        if (criteria.MinMonthlyAmount is not null || criteria.MaxMonthlyAmount is not null)
        {
            var range = new JsonObject();
            if (criteria.MinMonthlyAmount is { } min) range["gte"] = min;
            if (criteria.MaxMonthlyAmount is { } max) range["lte"] = max;
            filters.Add(new JsonObject { ["range"] = new JsonObject { ["monthlyAmount"] = range } });
        }

        return filters;
    }

    private static JsonArray Sort(SubscriptionSort sort, bool hasText) => sort switch
    {
        SubscriptionSort.Name => new JsonArray(new JsonObject { ["name.sort"] = "asc" }),
        SubscriptionSort.Amount => new JsonArray(new JsonObject { ["monthlyAmount"] = "desc" }, new JsonObject { ["name.sort"] = "asc" }),
        SubscriptionSort.StartDate => new JsonArray(new JsonObject { ["startDate"] = "desc" }, new JsonObject { ["name.sort"] = "asc" }),
        // Rilevanza senza testo: tutti i punteggi uguali → ordine alfabetico
        _ => hasText
            ? new JsonArray("_score", new JsonObject { ["name.sort"] = "asc" })
            : new JsonArray(new JsonObject { ["name.sort"] = "asc" })
    };

    private static JsonObject Terms(string field, int size) =>
        new() { ["terms"] = new JsonObject { ["field"] = field, ["size"] = size } };
}