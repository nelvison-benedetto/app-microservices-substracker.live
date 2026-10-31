using MongoDB.Bson.Serialization.Attributes;

namespace SubSnapMS.Notification.API.Persistence.Documents;

/// <summary>Destinatario: solo ciò che serve per scrivergli. Nessun altro dato personale.</summary>
public sealed class RecipientDocument
{
    [BsonId]
    public Guid UserId { get; set; }

    /// <summary>Null nei tombstone (utente cancellato).</summary>
    public string? Email { get; set; }

    /// <summary>Data dell'evento che ha impostato l'email: vince sempre la più recente.</summary>
    public DateTime EmailUpdatedAtUtc { get; set; }

    public bool Deleted { get; set; }

    /// <summary>Solo nei tombstone: l'indice TTL elimina il documento a questa data.</summary>
    public DateTime? ExpireAtUtc { get; set; }
}