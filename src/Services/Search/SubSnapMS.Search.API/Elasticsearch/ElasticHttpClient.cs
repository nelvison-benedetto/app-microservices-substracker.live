using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace SubSnapMS.Search.API.Elasticsearch;

//Client tipizzato per le poche API REST di Elasticsearch usate dal servizio.
//I path sono relativi (BaseAddress termina con "/"). Retry e timeout sono nel pipeline dell'HttpClient.
public sealed class ElasticHttpClient(HttpClient http)
{
    //camelCase, null omessi: è il formato dei documenti negli indici
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const int MaxErrorBodyLength = 1000;

    //HEAD: true se esiste (200), false se 404
    public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, path);
        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    //Crea un indice. False se esiste già (es. creato in parallelo da un'altra replica)
    public async Task<bool> CreateIndexAsync(string index, string definitionJson, CancellationToken cancellationToken)
    {
        using var content = new StringContent(definitionJson, Encoding.UTF8, "application/json");
        using var response = await http.PutAsync(index, content, cancellationToken);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Contains("resource_already_exists_exception", StringComparison.Ordinal))
                return false;

            throw new ElasticsearchException(response.StatusCode, Truncate(body));
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    //Indicizza con versione esterna: Elasticsearch accetta solo se version è MAGGIORE di quella salvata.
    //Restituisce false se rifiutato per versione (409): evento vecchio o duplicato.
    public async Task<bool> IndexWithExternalVersionAsync<TDocument>(
        string index, string id, TDocument document, long version, CancellationToken cancellationToken)
    {
        var path = $"{index}/_doc/{Uri.EscapeDataString(id)}?version={version}&version_type=external";
        using var response = await http.PutAsJsonAsync(path, document, JsonOptions, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
            return false;

        await EnsureSuccessAsync(response, cancellationToken);
        return true;
    }

    //Indicizza (crea o sostituisce) senza controllo di versione
    public async Task IndexAsync<TDocument>(string index, string id, TDocument document, CancellationToken cancellationToken)
    {
        using var response = await http.PutAsJsonAsync($"{index}/_doc/{Uri.EscapeDataString(id)}", document, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    //Cancella i documenti che soddisfano la query. conflicts=proceed: idempotente con scritture concorrenti
    public async Task<long> DeleteByQueryAsync(string index, JsonObject query, CancellationToken cancellationToken)
    {
        var body = new JsonObject { ["query"] = query };
        using var content = ToContent(body);
        using var response = await http.PostAsync($"{index}/_delete_by_query?conflicts=proceed", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var result = await ReadJsonAsync(response, cancellationToken);
        return result.RootElement.TryGetProperty("deleted", out var deleted) ? deleted.GetInt64() : 0;
    }

    public async Task<JsonDocument> SearchAsync(string index, JsonObject body, CancellationToken cancellationToken)
    {
        using var content = ToContent(body);
        using var response = await http.PostAsync($"{index}/_search", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await ReadJsonAsync(response, cancellationToken);
    }

    //"green", "yellow" o "red"
    public async Task<string?> GetClusterStatusAsync(CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync("_cluster/health", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        using var result = await ReadJsonAsync(response, cancellationToken);
        return result.RootElement.GetProperty("status").GetString();
    }

    private static StringContent ToContent(JsonNode body) =>
        new(body.ToJsonString(), Encoding.UTF8, "application/json");

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync(cancellationToken);
        throw new ElasticsearchException(response.StatusCode, Truncate(body));
    }

    private static string Truncate(string value) =>
        value.Length > MaxErrorBodyLength ? value[..MaxErrorBodyLength] : value;
}