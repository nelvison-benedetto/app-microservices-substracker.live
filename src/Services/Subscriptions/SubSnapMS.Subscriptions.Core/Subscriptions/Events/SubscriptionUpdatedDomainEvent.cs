using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Core.Subscriptions.Events;

/// <summary>Contiene lo stato COMPLETO dopo la modifica e la nuova Version.</summary>
public sealed record SubscriptionUpdatedDomainEvent(
    SubscriptionId SubscriptionId,
    UserId UserId,
    SubscriptionDetails Details,
    long Version,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;