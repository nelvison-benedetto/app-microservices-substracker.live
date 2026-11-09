using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SubSnapMS.Analytics.API.Calculations;

namespace SubSnapMS.Analytics.API.Persistence.Documents;

//Copia di un abbonamento: lo stato completo dell'ultimo evento applicato
public sealed class SubscriptionDocument
{
    [BsonId]
    public Guid SubscriptionId { get; set; }

    public Guid UserId { get; set; }

    //Version dell'evento applicato. Nei tombstone vale long.MaxValue
    public long Version { get; set; }

    public bool Deleted { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Category { get; set; }

    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    //Codice di Contracts.BillingCycles
    public string BillingCycle { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public DateTime? ExpireAtUtc { get; set; }

    public AnalyticsSubscription ToModel() => new(
        SubscriptionId,
        Name,
        Category,
        Amount,
        Currency,
        BillingCycle,
        StartDate.ToDateOnly(),
        EndDate?.ToDateOnly());
}