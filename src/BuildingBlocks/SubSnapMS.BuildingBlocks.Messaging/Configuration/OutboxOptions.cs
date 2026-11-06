namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

//Sezione "Messaging:Outbox"

public sealed class OutboxOptions
{
    public const string SectionName = "Messaging:Outbox";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(2);  //ogni quanto il processore cerca messaggi da pubblicare

    public int BatchSize { get; set; } = 50;

    public int MaxAttempts { get; set; } = 10;  //dopo questi tentativi falliti il messaggio resta nella tabella e va controllato a mano

    public TimeSpan Retention { get; set; } = TimeSpan.FromDays(7);  //per quanto tenere i messaggi già pubblicati prima di cancellarli

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}

