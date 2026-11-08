namespace SubSnapMS.Subscriptions.Application.Common.Results;

//Abbonamento + storico, dal più recente
public sealed record SubscriptionDetailResult(
    SubscriptionResult Subscription,
    IReadOnlyList<SubscriptionHistoryEntryResult> History);