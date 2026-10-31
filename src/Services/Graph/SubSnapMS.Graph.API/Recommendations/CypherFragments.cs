namespace SubSnapMS.Graph.API.Recommendations;

internal static class CypherFragments
{
    /// <summary>Abbonamento attivo oggi (inizio e fine incluse). Richiede il parametro $today ("yyyy-MM-dd").</summary>
    public static string Active(string alias) =>
        $"({alias}.startDate <= date($today) AND ({alias}.endDate IS NULL OR {alias}.endDate >= date($today)))";
}