using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Subscriptions;

/// <summary>
/// Abbonamento modificato: stato COMPLETO dopo la modifica (il consumer fa un upsert).
/// Version cresce a ogni modifica: un consumer ignora le versioni ≤ a quella che ha già (eventi fuori ordine).
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

    public required DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public required DateTimeOffset UpdatedOnUtc { get; init; }
}