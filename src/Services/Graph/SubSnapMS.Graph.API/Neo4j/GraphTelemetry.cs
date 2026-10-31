using System.Diagnostics;

namespace SubSnapMS.Graph.API.Neo4j;

/// <summary>Sorgente delle trace delle query Neo4j (esportata da Observability tramite AdditionalSources).</summary>
public static class GraphTelemetry
{
    public const string Name = "SubSnapMS.Graph";

    public static readonly ActivitySource ActivitySource = new(Name);
}