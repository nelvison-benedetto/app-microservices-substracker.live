using Microsoft.Extensions.Options;
using SubSnapMS.Notification.API.Configuration;

namespace SubSnapMS.Notification.API.Renewals;

//"Ora" e "oggi" nel fuso degli utenti (Europe/Rome), non in UTC: alle 00:30 italiane è già domani
public sealed class LocalCalendar(TimeProvider timeProvider, IOptions<NotificationOptions> options)
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.Reminders.TimeZone);

    public DateTimeOffset Now() => TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone);

    public DateOnly Today() => DateOnly.FromDateTime(Now().DateTime);
}