using Microsoft.Extensions.Options;
using SubSnapMS.Search.API.Configuration;

namespace SubSnapMS.Search.API.Querying;

//"Oggi" nel fuso degli utenti, per il filtro attivo/terminato
public sealed class SearchCalendar(TimeProvider timeProvider, IOptions<SearchOptions> options)
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone).DateTime);
}