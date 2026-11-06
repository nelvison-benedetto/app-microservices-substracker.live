namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

//Riga di inbox_messages: "questo handler ha già processato questo messaggio"

public sealed class InboxMessage
{
    private InboxMessage() { }   //constructor x EF Core

    internal InboxMessage(Guid messageId, string handlerName, DateTimeOffset processedOnUtc)
    {
        MessageId = messageId;
        HandlerName = handlerName;
        ProcessedOnUtc = processedOnUtc.ToUniversalTime();
    }  //constructor

    public Guid MessageId { get; private set; }
    public string HandlerName { get; private set; } = default!;
    public DateTimeOffset ProcessedOnUtc { get; private set; }

}

