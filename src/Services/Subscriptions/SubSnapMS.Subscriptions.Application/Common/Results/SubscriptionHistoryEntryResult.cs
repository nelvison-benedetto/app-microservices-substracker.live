using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Common.Results;

/// <summary>Una voce dello storico. Action: "created" | "updated".</summary>
public sealed record SubscriptionHistoryEntryResult(
    Guid Id,
    string Action,
    DateTimeOffset OccurredAtUtc,
    IReadOnlyList<SubscriptionFieldChangeResult> Changes)
{
    public static SubscriptionHistoryEntryResult From(SubscriptionHistoryEntry entry) => new(
        entry.Id.Value,
        entry.Action.ToString().ToLowerInvariant(),
        entry.OccurredAtUtc,
        entry.Changes.Select(SubscriptionFieldChangeResult.From).ToList());
}