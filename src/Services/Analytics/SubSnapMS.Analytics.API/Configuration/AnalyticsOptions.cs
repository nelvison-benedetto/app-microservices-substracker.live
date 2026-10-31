namespace SubSnapMS.Analytics.API.Configuration;

/// <summary>Sezione "Analytics".</summary>
public sealed class AnalyticsOptions
{
    public const string SectionName = "Analytics";

    /// <summary>Fuso degli utenti: decide cos'è "oggi" e il mese corrente.</summary>
    public string TimeZone { get; set; } = "Europe/Rome";

    /// <summary>Per quanto restano i tombstone di abbonamenti e utenti cancellati (poi li rimuove il TTL).</summary>
    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);
}