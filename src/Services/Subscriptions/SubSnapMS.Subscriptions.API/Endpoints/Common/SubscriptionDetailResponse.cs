using SubSnapMS.Subscriptions.Application.Common.Results;

namespace SubSnapMS.Subscriptions.API.Endpoints.Common;

/// <summary>Abbonamento + storico (dal più recente).</summary>
public sealed record SubscriptionDetailResponse(
    SubscriptionResponse Subscription,
    IReadOnlyList<SubscriptionHistoryEntryResponse> History)
{
    public static SubscriptionDetailResponse From(SubscriptionDetailResult result) => new(
        SubscriptionResponse.From(result.Subscription),
        result.History.Select(SubscriptionHistoryEntryResponse.From).ToList());
}

/// <summary>Una voce dello storico: "created" o "updated", con i campi cambiati.</summary>
public sealed record SubscriptionHistoryEntryResponse(
    Guid Id,
    string Action,
    DateTimeOffset OccurredAt,
    IReadOnlyList<SubscriptionFieldChangeResponse> Changes)
{
    public static SubscriptionHistoryEntryResponse From(SubscriptionHistoryEntryResult result) => new(
        result.Id,
        result.Action,
        result.OccurredAtUtc,
        result.Changes.Select(c => new SubscriptionFieldChangeResponse(c.Field, c.OldValue, c.NewValue)).ToList());
}

/// <summary>Es. { field: "amount", oldValue: "9.99", newValue: "12.99" }. Alla creazione oldValue è null.</summary>
public sealed record SubscriptionFieldChangeResponse(string Field, string? OldValue, string? NewValue);