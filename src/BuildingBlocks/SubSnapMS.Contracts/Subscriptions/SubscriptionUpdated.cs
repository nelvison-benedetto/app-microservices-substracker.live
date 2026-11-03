using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Subscriptions;

//abbonamento modificato: stato COMPLETO dopo la modifica (il consumer fa un upsert, non ricostruisce nulla).
//un consumer ignora un evento con Version ≤ a quella che ha già: protegge dagli eventi arrivati fuori ordine.

[IntegrationEvent(Exchanges.Subscriptions, "subscription.updated.v1")]
public sealed record SubscriptionUpdated : IntegrationEvent
{
    public required Guid SubscriptionId { get; init; }

    public required Guid UserId { get; init; }

    public required long Version { get; init; }

    public required string Name { get; init; }

    public string? Category { get; init; }

    public required decimal Amount { get; init; }

    public required string Currency { get; init; }

    public required string BillingCycle { get; init; }

    public required DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public required DateTimeOffset UpdatedOnUtc { get; init; }
}

