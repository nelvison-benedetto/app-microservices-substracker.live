using MongoDB.Bson.Serialization.Attributes;

namespace SubSnapMS.Notification.API.Persistence.Documents;

//Destinatario: solo ciò che serve per scrivergli. Nessun altro dato personale
public sealed class RecipientDocument
{
    [BsonId]
    public Guid UserId { get; set; }

    //Null nei tombstone (utente cancellato)
    public string? Email { get; set; }

    //Data dell'evento che ha impostato l'email: vince sempre la più recente
    public DateTime EmailUpdatedAtUtc { get; set; }

    public bool Deleted { get; set; }

    //Solo nei tombstone: l'indice TTL elimina il documento a questa data
    public DateTime? ExpireAtUtc { get; set; }
}