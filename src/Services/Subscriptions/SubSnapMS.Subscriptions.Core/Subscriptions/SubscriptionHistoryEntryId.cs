namespace SubSnapMS.Subscriptions.Core.Subscriptions;

public readonly record struct SubscriptionHistoryEntryId(Guid Value)
{
    public static SubscriptionHistoryEntryId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}