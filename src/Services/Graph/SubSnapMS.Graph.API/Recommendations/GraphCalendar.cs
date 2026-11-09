using System.Globalization;
using Microsoft.Extensions.Options;
using SubSnapMS.Graph.API.Configuration;

namespace SubSnapMS.Graph.API.Recommendations;

//"Oggi" nel fuso degli utenti, nel formato atteso da date() in Cypher
public sealed class GraphCalendar(TimeProvider timeProvider, IOptions<GraphOptions> options)
{
    private readonly TimeZoneInfo _timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);

    public string Today() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), _timeZone).DateTime)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}