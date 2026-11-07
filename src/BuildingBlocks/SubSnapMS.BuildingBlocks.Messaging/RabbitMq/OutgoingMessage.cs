namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;


//è il pacchetto standard che arriva al punto finale di invio verso RabbitMQ, contiene tutto ciò che serve per pubblicare (è gia serializzato) e niente di più
internal sealed record OutgoingMessage(
    Guid MessageId,
    string Exchange,
    string EventName,
    ReadOnlyMemory<byte> Body,
    DateTimeOffset OccurredOnUtc,
    string? TraceParent,
    string? TraceState);

