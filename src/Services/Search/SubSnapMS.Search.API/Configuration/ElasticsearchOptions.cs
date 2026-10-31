namespace SubSnapMS.Search.API.Configuration;

/// <summary>
/// Sezione "Elasticsearch". Autenticazione: ApiKey (consigliata) oppure Username/Password.
/// Segreti da variabili d'ambiente: Elasticsearch__ApiKey, Elasticsearch__Password.
/// </summary>
public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    public string Uri { get; set; } = "http://elasticsearch:9200";

    public string? ApiKey { get; set; }

    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>0 con un solo nodo (altrimenti il cluster resta "yellow"). Usata solo alla creazione degli indici.</summary>
    public int NumberOfReplicas { get; set; }
}