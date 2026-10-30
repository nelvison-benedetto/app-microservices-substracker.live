using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

/// <summary>Publish di basso livello, usata dall'OutboxProcessor.</summary>
internal interface IMessagePublisher
{
    Task PublishRawAsync(OutgoingMessage message, CancellationToken cancellationToken = default);
}