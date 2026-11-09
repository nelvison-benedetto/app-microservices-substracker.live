using MongoDB.Bson.Serialization.Attributes;

namespace SubSnapMS.Notification.API.Persistence.Documents;

public enum NotificationStatus
{
    Sending,
    Sent,
    Failed
}

//Una notifica (inviata o in invio). L'_id è la chiave di deduplica (es. "welcome:{userId}").
//Nessun indirizzo email salvato: solo l'id utente.
public sealed class NotificationLogDocument
{
    [BsonId]
    public string Key { get; set; } = string.Empty;

    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public NotificationStatus Status { get; set; }

    public int Attempts { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    //Inizio del tentativo in corso: serve a riprendere un invio rimasto bloccato
    public DateTime ClaimedAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public string? LastError { get; set; }
}