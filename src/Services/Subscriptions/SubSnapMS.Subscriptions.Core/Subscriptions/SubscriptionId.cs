using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions;

public readonly record struct SubscriptionId(Guid Value)
{
    public static SubscriptionId New() => new(Guid.CreateVersion7());

    public static SubscriptionId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException("subscription_id.empty", "Subscription id cannot be empty.")
            : new SubscriptionId(value);

    public override string ToString() => Value.ToString();
}