using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

/// <summary>Riga di inbox_messages: "questo handler ha già processato questo messaggio".</summary>
public sealed class InboxMessage
{
    private InboxMessage() { }   // per EF Core

    internal InboxMessage(Guid messageId, string handlerName, DateTimeOffset processedOnUtc)
    {
        MessageId = messageId;
        HandlerName = handlerName;
        ProcessedOnUtc = processedOnUtc.ToUniversalTime();
    }

    public Guid MessageId { get; private set; }
    public string HandlerName { get; private set; } = default!;
    public DateTimeOffset ProcessedOnUtc { get; private set; }
}