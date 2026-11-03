using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Subscriptions;

//Abbonamento cancellato dall'utente. NON viene pubblicato per ogni abbonamento durante la cancellazione dell'account: in quel caso i consumer reagiscono a UserDeletionRequested.

[IntegrationEvent(Exchanges.Subscriptions, "subscription.deleted.v1")]
public sealed record SubscriptionDeleted : IntegrationEvent
{
    public required Guid SubscriptionId { get; init; }

    public required Guid UserId { get; init; }

    public required DateTimeOffset DeletedOnUtc { get; init; }
}

