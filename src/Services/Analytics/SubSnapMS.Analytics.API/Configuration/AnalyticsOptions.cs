namespace SubSnapMS.Analytics.API.Configuration;

//Sezione "Analytics"
public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    //Fuso degli utenti: decide cos'è "oggi" e il mese corrente
    public string TimeZone { get; set; } = "Europe/Rome";

    //Per quanto restano i tombstone di abbonamenti e utenti cancellati (poi li rimuove il TTL)
    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);
}