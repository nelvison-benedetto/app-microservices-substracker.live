namespace SubSnapMS.Search.API.Configuration;

//Sezione "Search"
public sealed class SearchOptions
{
    public const string SectionName = "Search";

    //Fuso degli utenti: decide cos'è "oggi" per il filtro attivo/terminato
    public string TimeZone { get; set; } = "Europe/Rome";

    //Dopo quanto vengono rimossi i tombstone di abbonamenti e utenti cancellati
    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
}