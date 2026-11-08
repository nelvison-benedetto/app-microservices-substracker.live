using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Core.Subscriptions.Events;

//Contiene lo stato COMPLETO dopo la modifica e la nuova Version
public sealed record SubscriptionUpdatedDomainEvent(
    SubscriptionId SubscriptionId,
    UserId UserId,
    SubscriptionDetails Details,
    long Version,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;