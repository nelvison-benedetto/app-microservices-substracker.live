namespace SubSnapMS.Graph.API.Configuration;

/// <summary>Sezione "Neo4j". Password da variabile d'ambiente: Neo4j__Password.</summary>
public sealed class Neo4jOptions
{
    public const string SectionName = "Neo4j";

    /// <summary>bolt://neo4j:7687 (istanza singola) o neo4j://… (cluster, con routing).</summary>
    public string Uri { get; set; } = "bolt://neo4j:7687";

    public string Username { get; set; } = "neo4j";

    public string Password { get; set; } = string.Empty;

    /// <summary>Neo4j Community ha un solo database utente: "neo4j".</summary>
    public string Database { get; set; } = "neo4j";

    public int MaxConnectionPoolSize { get; set; } = 50;
}