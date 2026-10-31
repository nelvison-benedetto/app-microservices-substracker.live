namespace SubSnapMS.Graph.API.Configuration;

/// <summary>Sezione "Graph".</summary>
public sealed class GraphOptions
{
    public const string SectionName = "Graph";

    /// <summary>Fuso degli utenti: decide quali abbonamenti sono attivi "oggi".</summary>
    public string TimeZone { get; set; } = "Europe/Rome";

    /// <summary>
    /// Privacy: un servizio viene raccomandato solo se lo hanno almeno N altri utenti.
    /// Impedisce di dedurre gli abbonamenti di un singolo utente e di mostrare nomi privati.
    /// </summary>
    public int MinUsersForRecommendation { get; set; } = 3;

    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
}