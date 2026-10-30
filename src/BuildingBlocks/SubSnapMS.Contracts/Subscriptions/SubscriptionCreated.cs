using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Subscriptions;

/// <summary>
/// Nuovo abbonamento. Contiene lo stato completo, così i read model non devono richiamare Subscriptions.
/// Consumer tipici: Analytics, Search, Graph, Notification.
/// </summary>
[IntegrationEvent(Exchanges.Subscriptions, "subscription.created.v1")]
public sealed record SubscriptionCreated : IntegrationEvent
{
    public required Guid SubscriptionId { get; init; }

    public required Guid UserId { get; init; }

    /// <summary>Nome del servizio, es. "Netflix".</summary>
    public required string Name { get; init; }

    public string? Category { get; init; }

    public required decimal Amount { get; init; }

    /// <summary>Codice ISO 4217, es. "EUR".</summary>
    public required string Currency { get; init; }

    /// <summary>Uno dei valori di <see cref="BillingCycles"/>.</summary>
    public required string BillingCycle { get; init; }

    public required DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public required DateTimeOffset CreatedOnUtc { get; init; }
}