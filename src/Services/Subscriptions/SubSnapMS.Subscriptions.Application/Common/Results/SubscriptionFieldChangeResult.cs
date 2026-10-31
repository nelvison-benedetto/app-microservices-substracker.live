using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Common.Results;

/// <summary>Un campo cambiato. Valori in formato invariante: "12.99", "2026-04-01", "monthly".</summary>
public sealed record SubscriptionFieldChangeResult(string Field, string? OldValue, string? NewValue)
{
    public static SubscriptionFieldChangeResult From(SubscriptionFieldChange change) =>
        new(change.Field, change.OldValue, change.NewValue);
}