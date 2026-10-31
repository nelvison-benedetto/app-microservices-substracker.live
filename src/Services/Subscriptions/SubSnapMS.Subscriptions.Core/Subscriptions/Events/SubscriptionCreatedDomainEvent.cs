using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Core.Subscriptions.Events;

public sealed record SubscriptionCreatedDomainEvent(
    SubscriptionId SubscriptionId,
    UserId UserId,
    SubscriptionDetails Details,
    long Version,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;