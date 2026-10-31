namespace SubSnapMS.Subscriptions.Application.Common.Results;

/// <summary>Abbonamento + storico, dal più recente.</summary>
public sealed record SubscriptionDetailResult(
    SubscriptionResult Subscription,
    IReadOnlyList<SubscriptionHistoryEntryResult> History);