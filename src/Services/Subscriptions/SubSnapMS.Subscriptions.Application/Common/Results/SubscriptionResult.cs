using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Common.Results;

//Un abbonamento in uscita dall'Application. BillingCycle come codice neutro ("monthly"...)
public sealed record SubscriptionResult(
    Guid Id,
    string Name,
    string? Category,
    decimal Amount,
    string Currency,
    string BillingCycle,
    decimal MonthlyAmount,
    DateOnly StartDate,
    DateOnly? EndDate,
    long Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc)
{
    public static SubscriptionResult From(Subscription subscription) => new(
        subscription.Id.Value,
        subscription.Details.Name,
        subscription.Details.Category,
        subscription.Details.Price.Amount,
        subscription.Details.Price.Currency,
        subscription.Details.BillingCycle.ToCode(),
        subscription.MonthlyCost.Amount,
        subscription.Details.Period.StartDate,
        subscription.Details.Period.EndDate,
        subscription.Version,
        subscription.CreatedAtUtc,
        subscription.UpdatedAtUtc);
}