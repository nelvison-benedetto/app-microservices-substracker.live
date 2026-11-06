namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

//Sezione "Messaging:Inbox"

public sealed class InboxOptions
{
    public const string SectionName = "Messaging:Inbox";

    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(30);  //Per quanto ricordare un messaggio già processato. Deve superare di molto il tempo in cui un duplicato può ancora arrivare (retry dell'outbox, riconsegne, replay dalla DLQ)

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(6);  //ogni quanto fare pulizie, viene usato da InboxCleanupService il service in background che esegue la cancellazione.

}