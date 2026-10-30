using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Subscriptions;

/// <summary>
/// Abbonamento modificato. Porta lo stato COMPLETO dopo la modifica (non solo i campi cambiati):
/// il consumer fa un upsert e non deve ricostruire nulla.
/// Version cresce a ogni modifica: un consumer che riceve una versione ≤ a quella che ha già la ignora
/// (protegge dagli eventi arrivati fuori ordine).
/// </summary>
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

    public DateOnly? NextBillingDate { get; init; }

    public required DateTimeOffset UpdatedOnUtc { get; init; }
}