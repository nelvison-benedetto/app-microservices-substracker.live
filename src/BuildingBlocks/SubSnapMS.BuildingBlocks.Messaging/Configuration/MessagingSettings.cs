using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

/// <summary>Nomi della topologia RabbitMQ derivati dal nome del servizio.</summary>
internal sealed record MessagingSettings(string ServiceName)
{
    /// <summary>Coda del servizio, legata agli exchange degli eventi a cui è iscritto.</summary>
    public string QueueName => $"{ServiceName}.queue";

    public string DeadLetterExchange => $"{ServiceName}.dlx";

    public string DeadLetterQueue => $"{ServiceName}.queue.dlq";
}