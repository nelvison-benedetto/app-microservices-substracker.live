using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Diagnostics;

/// <summary>
/// Trace e metriche del messaging. Il nome inizia con "SubSnapMS." quindi Observability
/// li raccoglie in automatico (AddSource / AddMeter "SubSnapMS.*").
/// </summary>
internal static class MessagingDiagnostics
{
    public const string Name = "SubSnapMS.Messaging";

    public const string OutcomeSuccess = "success";
    public const string OutcomeDeadLettered = "dead_lettered";
    public const string OutcomeDiscarded = "discarded";

    public static readonly ActivitySource ActivitySource = new(Name);

    private static readonly Meter Meter = new(Name);

    public static readonly Counter<long> PublishedMessages = Meter.CreateCounter<long>(
        "subsnapms.messaging.published", unit: "{message}", description: "Messaggi confermati da RabbitMQ.");

    public static readonly Counter<long> OutboxPublishFailures = Meter.CreateCounter<long>(
        "subsnapms.messaging.outbox.failures", unit: "{message}", description: "Tentativi di pubblicazione dall'Outbox falliti.");

    public static readonly Counter<long> ProcessedMessages = Meter.CreateCounter<long>(
        "subsnapms.messaging.processed", unit: "{message}", description: "Messaggi ricevuti, per esito (success, dead_lettered, discarded).");

    public static readonly Histogram<double> ProcessDuration = Meter.CreateHistogram<double>(
        "subsnapms.messaging.process.duration", unit: "s", description: "Durata dell'elaborazione di un messaggio, retry inclusi.");

    /// <summary>Ricostruisce il contesto della trace da un traceparent W3C (default se assente o non valido).</summary>
    public static ActivityContext ParseParent(string? traceParent, string? traceState) =>
        traceParent is not null && ActivityContext.TryParse(traceParent, traceState, out var context)
            ? context
            : default;

    public static void RecordError(Activity? activity, Exception exception)
    {
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
    }

    public static void RecordProcessed(string eventName, string outcome, long startTimestamp)
    {
        var tags = new TagList
        {
            { "messaging.event.name", eventName },
            { "messaging.outcome", outcome }
        };

        ProcessedMessages.Add(1, tags);
        ProcessDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds, tags);
    }
}