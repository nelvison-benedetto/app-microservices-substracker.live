namespace SubSnapMS.Notification.API.Configuration;

/// <summary>Sezione "Notification".</summary>
public sealed class NotificationOptions
{
    public const string SectionName = "Notification";

    public WelcomeOptions Welcome { get; set; } = new();

    public ReminderOptions Reminders { get; set; } = new();

    public DeliveryOptions Delivery { get; set; } = new();

    /// <summary>Per quanto restano i tombstone di utenti e abbonamenti cancellati (poi li rimuove il TTL di Mongo).</summary>
    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);
}

public sealed class WelcomeOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Registrazioni più vecchie di così non ricevono il benvenuto (backfill, replay dalla DLQ).</summary>
    public TimeSpan MaxEventAge { get; set; } = TimeSpan.FromDays(2);
}

public sealed class ReminderOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>Quanti giorni prima del rinnovo inviare il promemoria.</summary>
    public int DaysBefore { get; set; } = 3;

    /// <summary>Fuso orario degli utenti: decide cos'è "oggi" e la fascia oraria di invio.</summary>
    public string TimeZone { get; set; } = "Europe/Rome";

    /// <summary>Niente email di notte: invio solo tra SendFromHour (incluso) e SendUntilHour (escluso), ora locale.</summary>
    public int SendFromHour { get; set; } = 8;

    public int SendUntilHour { get; set; } = 21;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Promemoria massimi per giro del job (il resto al giro successivo).</summary>
    public int BatchSize { get; set; } = 500;
}

public sealed class DeliveryOptions
{
    /// <summary>Tentativi di invio massimi per la stessa notifica, poi rinuncia (stato Failed).</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>Dopo quanto un invio rimasto "Sending" (es. crash) può essere ripreso da un altro tentativo.</summary>
    public TimeSpan ClaimLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Conservazione del registro invii (indice TTL su createdAtUtc).</summary>
    public TimeSpan LogRetention { get; set; } = TimeSpan.FromDays(180);
}