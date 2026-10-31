using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SubSnapMS.Notification.API.Persistence.Documents;

/// <summary>Copia di un abbonamento, con il prossimo rinnovo pre-calcolato per il job dei promemoria.</summary>
public sealed class SubscriptionDocument
{
    [BsonId]
    public Guid SubscriptionId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Version dell'evento applicato. Nei tombstone vale long.MaxValue: nessun evento successivo lo sovrascrive.</summary>
    public long Version { get; set; }

    public bool Deleted { get; set; }

    public string Name { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    /// <summary>Codice di Contracts.BillingCycles.</summary>
    public string BillingCycle { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    /// <summary>Null se l'abbonamento è terminato (EndDate superata) o il ciclo è sconosciuto.</summary>
    public DateTime? NextRenewalDate { get; set; }

    /// <summary>True finché il promemoria per NextRenewalDate non è stato gestito.</summary>
    public bool ReminderPending { get; set; }

    public DateTime? ExpireAtUtc { get; set; }
}