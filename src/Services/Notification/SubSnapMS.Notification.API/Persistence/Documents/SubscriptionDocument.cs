using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SubSnapMS.Notification.API.Persistence.Documents;

//Copia di un abbonamento, con il prossimo rinnovo pre-calcolato per il job dei promemoria
public sealed class SubscriptionDocument
{
    [BsonId]
    public Guid SubscriptionId { get; set; }

    public Guid UserId { get; set; }

    //Version dell'evento applicato. Nei tombstone vale long.MaxValue: nessun evento successivo lo sovrascrive
    public long Version { get; set; }

    public bool Deleted { get; set; }

    public string Name { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    //Codice di Contracts.BillingCycles
    public string BillingCycle { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    //Null se l'abbonamento è terminato (EndDate superata) o il ciclo è sconosciuto
    public DateTime? NextRenewalDate { get; set; }

    //True finché il promemoria per NextRenewalDate non è stato gestito
    public bool ReminderPending { get; set; }

    public DateTime? ExpireAtUtc { get; set; }
}