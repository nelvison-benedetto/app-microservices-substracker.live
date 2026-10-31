using System.Text.Json.Nodes;

namespace SubSnapMS.Search.API.Elasticsearch;

/// <summary>
/// Piccoli costruttori per il Query DSL. Ogni chiamata crea nodi nuovi:
/// un JsonNode può avere un solo genitore, quindi non va riusato in due punti della query.
/// </summary>
public static class EsQuery
{
    public static JsonObject Term(string field, JsonNode? value) =>
        new() { ["term"] = new JsonObject { [field] = value } };

    public static JsonObject Range(string field, string @operator, JsonNode? value) =>
        new() { ["range"] = new JsonObject { [field] = new JsonObject { [@operator] = value } } };

    public static JsonObject Exists(string field) =>
        new() { ["exists"] = new JsonObject { ["field"] = field } };

    public static JsonObject Bool(
        JsonArray? filter = null,
        JsonArray? must = null,
        JsonArray? should = null,
        JsonArray? mustNot = null,
        int? minimumShouldMatch = null)
    {
        var body = new JsonObject();
        if (filter is { Count: > 0 }) body["filter"] = filter;
        if (must is { Count: > 0 }) body["must"] = must;
        if (should is { Count: > 0 }) body["should"] = should;
        if (mustNot is { Count: > 0 }) body["must_not"] = mustNot;
        if (minimumShouldMatch is not null) body["minimum_should_match"] = minimumShouldMatch;

        return new JsonObject { ["bool"] = body };
    }
}