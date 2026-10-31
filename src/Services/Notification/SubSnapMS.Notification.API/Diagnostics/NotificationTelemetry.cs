using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SubSnapMS.Notification.API.Diagnostics;

/// <summary>Trace e metriche del servizio (esportate da Observability tramite AdditionalSources/AdditionalMeters).</summary>
public static class NotificationTelemetry
{
    public const string Name = "SubSnapMS.Notification";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter Meter = new(Name);

    private static readonly Counter<long> Emails = Meter.CreateCounter<long>(
        "subsnapms.notification.emails",
        description: "Email per tipo e risultato (sent, failed, skipped).");

    public static void RecordEmail(string type, string result) =>
        Emails.Add(1, new KeyValuePair<string, object?>("type", type), new KeyValuePair<string, object?>("result", result));
}