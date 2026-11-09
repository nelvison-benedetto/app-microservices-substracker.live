namespace SubSnapMS.Graph.API.Configuration;

//Sezione "Graph"
public sealed class GraphOptions
{
    public const string SectionName = "Graph";

    //Fuso degli utenti: decide quali abbonamenti sono attivi "oggi"
    public string TimeZone { get; set; } = "Europe/Rome";

    //Privacy: un servizio viene raccomandato solo se lo hanno almeno N altri utenti.
    //Impedisce di dedurre gli abbonamenti di un singolo utente e di mostrare nomi privati.
    public int MinUsersForRecommendation { get; set; } = 3;

    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
}