using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

/// <summary>Messaggio già serializzato, pronto per il broker (dall'Outbox o da una publish diretta).</summary>
internal sealed record OutgoingMessage(
    Guid MessageId,
    string Exchange,
    string EventName,
    ReadOnlyMemory<byte> Body,
    DateTimeOffset OccurredOnUtc,
    string? TraceParent,
    string? TraceState);