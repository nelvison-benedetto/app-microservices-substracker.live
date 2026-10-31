using Microsoft.Extensions.Options;
using SubSnapMS.Analytics.API.Configuration;

namespace SubSnapMS.Analytics.API.Calculations;

/// <summary>"Oggi" nel fuso degli utenti: alle 00:30 italiane del 1° del mese il mese corrente è già il nuovo.</summary>
public sealed class AnalyticsCalendar(TimeProvider timeProvider, IOptions<AnalyticsOptions> options)
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone).DateTime);
}