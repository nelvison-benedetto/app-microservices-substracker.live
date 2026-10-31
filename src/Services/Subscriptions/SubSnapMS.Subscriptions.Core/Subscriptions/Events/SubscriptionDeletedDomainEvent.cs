using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Core.Subscriptions.Events;

public sealed record SubscriptionDeletedDomainEvent(
    SubscriptionId SubscriptionId,
    UserId UserId,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;