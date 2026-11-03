using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Subscriptions;

//nuovo abbonamento. Contiene lo stato completo, così i read model non devono richiamare Subscriptions.
//consumers: Analytics (dashboard), Search (indicizzazione), Graph (relazione User→Service), Notification (promemoria di rinnovo).

[IntegrationEvent(Exchanges.Subscriptions, "subscription.created.v1")]
public sealed record SubscriptionCreated : IntegrationEvent
{
    public required Guid SubscriptionId { get; init; }

    public required Guid UserId { get; init; }


    public required long Version { get; init; }  //version dello stato, 1 alla creazione, poi cresce a ogni SubscriptionUpdated

    public required string Name { get; init; }  //e.g. "Netflix", "Spotify", "Amazon Prime Video"

    public string? Category { get; init; }

    public required decimal Amount { get; init; }

    public required string Currency { get; init; }  ///coice ISO 4217 e.g. "EUR", "USD", "GBP"

    public required string BillingCycle { get; init; }

    public required DateOnly StartDate { get; init; }

    public DateOnly? EndDate { get; init; }

    public required DateTimeOffset CreatedOnUtc { get; init; }


}