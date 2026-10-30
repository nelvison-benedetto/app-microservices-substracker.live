using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

/// <summary>Sezione "Messaging:Inbox".</summary>
public sealed class InboxOptions
{
    public const string SectionName = "Messaging:Inbox";

    /// <summary>
    /// Per quanto ricordare un messaggio già processato. Deve superare di molto il tempo
    /// in cui un duplicato può ancora arrivare (retry dell'outbox, riconsegne, replay dalla DLQ).
    /// </summary>
    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(6);
}