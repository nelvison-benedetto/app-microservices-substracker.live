namespace SubSnapMS.Search.API.Configuration;

/// <summary>Sezione "Search".</summary>
public sealed class SearchOptions
{
    public const string SectionName = "Search";

    /// <summary>Fuso degli utenti: decide cos'è "oggi" per il filtro attivo/terminato.</summary>
    public string TimeZone { get; set; } = "Europe/Rome";

    /// <summary>Dopo quanto vengono rimossi i tombstone di abbonamenti e utenti cancellati.</summary>
    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromDays(1);
}