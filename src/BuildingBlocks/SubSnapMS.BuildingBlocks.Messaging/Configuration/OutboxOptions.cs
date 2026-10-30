using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

/// <summary>Sezione "Messaging:Outbox".</summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Messaging:Outbox";

    /// <summary>Ogni quanto il processore cerca messaggi da pubblicare.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; set; } = 50;

    /// <summary>Dopo questi tentativi falliti il messaggio resta nella tabella e va controllato a mano.</summary>
    public int MaxAttempts { get; set; } = 10;

    /// <summary>Per quanto tenere i messaggi già pubblicati prima di cancellarli.</summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}
