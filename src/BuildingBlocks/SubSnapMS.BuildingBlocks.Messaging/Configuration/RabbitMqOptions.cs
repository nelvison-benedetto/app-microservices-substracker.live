using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

/// <summary>Sezione "Messaging:RabbitMq" (variabili d'ambiente: Messaging__RabbitMq__Host, ...).</summary>
public sealed class RabbitMqOptions
{
    public const string SectionName = "Messaging:RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";

    /// <summary>Messaggi che RabbitMQ consegna in anticipo al consumer (buffer).</summary>
    public ushort PrefetchCount { get; set; } = 16;

    /// <summary>Retry di un handler dopo il primo tentativo fallito. Poi il messaggio va in DLQ.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Backoff esponenziale: base, base×2, base×4, ...</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);
}