using MongoDB.Bson.Serialization.Attributes;

namespace SubSnapMS.Analytics.API.Persistence.Documents;

/// <summary>Tombstone di un utente cancellato: nessun dato personale, solo l'id e la scadenza (TTL).</summary>
public sealed class DeletedUserDocument
{
    [BsonId]
    public Guid UserId { get; set; }

    public DateTime DeletedAtUtc { get; set; }

    public DateTime ExpireAtUtc { get; set; }
}