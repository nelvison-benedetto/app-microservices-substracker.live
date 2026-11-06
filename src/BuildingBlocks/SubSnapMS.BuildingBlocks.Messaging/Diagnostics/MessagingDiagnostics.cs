using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace SubSnapMS.BuildingBlocks.Messaging.Diagnostics;

//race e metriche del messaging. Il nome inizia con "SubSnapMS." quindi Observability li raccoglie in automatico (AddSource / AddMeter "SubSnapMS.*").

internal static class MessagingDiagnostics
{
    public const string Name = "SubSnapMS.Messaging";

    //3 pissibili esiti di un mex ricevuto
    public const string OutcomeSuccess = "success";
    public const string OutcomeDeadLettered = "dead_lettered";
    public const string OutcomeDiscarded = "discarded";


    public static readonly ActivitySource ActivitySource = new(Name);  //X GLI SPAN
    private static readonly Meter Meter = new(Name);  //X LE METRICS


    public static readonly Counter<long> PublishedMessages = Meter.CreateCounter<long>(
        "subsnapms.messaging.published", unit: "{message}", description: "Messaggi confermati da RabbitMQ.");

    public static readonly Counter<long> OutboxPublishFailures = Meter.CreateCounter<long>(
        "subsnapms.messaging.outbox.failures", unit: "{message}", description: "Tentativi di pubblicazione dall'Outbox falliti.");  //è un counter, che incrementa di 1 solo dopo che rabbitmq ha confermato di aver preso in carico il mex (publisher confirm)

    public static readonly Counter<long> ProcessedMessages = Meter.CreateCounter<long>(
        "subsnapms.messaging.processed", unit: "{message}", description: "Messaggi ricevuti, per esito (success, dead_lettered, discarded)."); //mexs ricevuti dai consumers, divisi per evento ed esito.

    public static readonly Histogram<double> ProcessDuration = Meter.CreateHistogram<double>(
        "subsnapms.messaging.process.duration", unit: "s", description: "Durata dell'elaborazione di un messaggio, retry inclusi.");  //x e.g. “il 95% dei messaggi viene elaborato in meno di 120 ms”


    public static ActivityContext ParseParent(string? traceParent, string? traceState) =>
        traceParent is not null && ActivityContext.TryParse(traceParent, traceState, out var context)
            ? context
            : default;
    //ricostruisce il contesto del tracing da un traceparent (stringa in formato W3C) (default se assente o non valido). COSI IL TRACING è DISPONIBILE ANCHE IN COMUNICAZIONI ASYNC, invece in comunicazioni sync e.g.httpclient aggiunge il traceparent all'header

    public static void RecordError(Activity? activity, Exception exception)
    {
        if (activity is null) return;

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddException(exception);
    }
    //stessa logica di TracingBehavior: segna lo span come fallito e allega l’eccezione (tipo, messaggio, stack trace). Il consumer la chiama quando un messaggio finisce in DLQ.

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
    //consumero call this una volta per mex, alla fine qualche sia stato l'esito


}