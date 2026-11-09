namespace SubSnapMS.Notification.API.Configuration;

//Sezione "Notification"
public sealed class NotificationOptions
{
    public const string SectionName = "Notification";

    public WelcomeOptions Welcome { get; set; } = new();

    public ReminderOptions Reminders { get; set; } = new();

    public DeliveryOptions Delivery { get; set; } = new();

    //Per quanto restano i tombstone di utenti e abbonamenti cancellati (poi li rimuove il TTL di Mongo)
    public TimeSpan DeletedTombstoneRetention { get; set; } = TimeSpan.FromDays(30);
}

public sealed class WelcomeOptions
{
    public bool Enabled { get; set; } = true;

    //Registrazioni più vecchie di così non ricevono il benvenuto (backfill, replay dalla DLQ)
    public TimeSpan MaxEventAge { get; set; } = TimeSpan.FromDays(2);
}

public sealed class ReminderOptions
{
    public bool Enabled { get; set; } = true;

    //Quanti giorni prima del rinnovo inviare il promemoria
    public int DaysBefore { get; set; } = 3;

    //Fuso orario degli utenti: decide cos'è "oggi" e la fascia oraria di invio
    public string TimeZone { get; set; } = "Europe/Rome";

    //Niente email di notte: invio solo tra SendFromHour (incluso) e SendUntilHour (escluso), ora locale
    public int SendFromHour { get; set; } = 8;

    public int SendUntilHour { get; set; } = 21;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(30);

    //Promemoria massimi per giro del job (il resto al giro successivo)
    public int BatchSize { get; set; } = 500;
}

public sealed class DeliveryOptions
{
    //Tentativi di invio massimi per la stessa notifica, poi rinuncia (stato Failed)
    public int MaxAttempts { get; set; } = 5;

    //Dopo quanto un invio rimasto "Sending" (es. crash) può essere ripreso da un altro tentativo
    public TimeSpan ClaimLease { get; set; } = TimeSpan.FromMinutes(5);

    //Conservazione del registro invii (indice TTL su createdAtUtc)
    public TimeSpan LogRetention { get; set; } = TimeSpan.FromDays(180);
}