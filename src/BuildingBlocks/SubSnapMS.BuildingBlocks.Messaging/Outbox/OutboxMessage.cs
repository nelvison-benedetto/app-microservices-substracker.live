namespace SubSnapMS.BuildingBlocks.Messaging.Outbox;

//Riga della tabella outbox_messages: un evento in attesa di essere pubblicato
public sealed class OutboxMessage
{
    private const int MaxErrorLength = 2000;

    private OutboxMessage() { }   //per EF Core

    public Guid Id { get; private set; }                       //key della row, MessageId su rabbitmq, key dell'Inbox del consumer
    public string EventName { get; private set; } = default!;  //routing key della pubblicazione e.g.subscription.created.v1
    public string Exchange { get; private set; } = default!;  //dove pubblicare
    public string Payload { get; private set; } = default!;    //corpo del mex
    public string? TraceParent { get; private set; }           //trace della richiesta che l'ha generato
    public string? TraceState { get; private set; }  //span di publish continua quella trace
    public DateTimeOffset OccurredOnUtc { get; private set; }  //ordine di pubblicazione, timestamp del mex
    public DateTimeOffset? ProcessedOnUtc { get; private set; }  //"da pubblicare?" e pulizia dopo 7 giorni
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    internal static OutboxMessage Create(
        Guid id, string eventName, string exchange, string payload,
        DateTimeOffset occurredOnUtc, string? traceParent, string? traceState) => new()
        {
            Id = id,
            EventName = eventName,
            Exchange = exchange,
            Payload = payload,
            OccurredOnUtc = occurredOnUtc.ToUniversalTime(),   // Npgsql accetta solo offset 0 su timestamptz
            TraceParent = traceParent,
            TraceState = traceState
        };

    internal void MarkProcessed(DateTimeOffset processedOnUtc)
    {
        ProcessedOnUtc = processedOnUtc.ToUniversalTime();
        LastError = null;
    }

    internal void MarkFailed(Exception exception)
    {
        Attempts++;
        var message = $"{exception.GetType().Name}: {exception.Message}";
        LastError = message.Length <= MaxErrorLength ? message : message[..MaxErrorLength];
    }
}


