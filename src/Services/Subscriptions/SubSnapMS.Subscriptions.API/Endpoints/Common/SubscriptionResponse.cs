using SubSnapMS.Subscriptions.Application.Common.Results;

namespace SubSnapMS.Subscriptions.API.Endpoints.Common;

//Un abbonamento come lo riceve il frontend. Stessi campi del old monolith SubSnap (id, name, amount, billingCycle, startDate, endDate, category, createdAt, updatedAt) + currency, monthlyAmount e version.
public sealed record SubscriptionResponse(
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
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static SubscriptionResponse From(SubscriptionResult result) => new(
        result.Id,
        result.Name,
        result.Category,
        result.Amount,
        result.Currency,
        result.BillingCycle,
        result.MonthlyAmount,
        result.StartDate,
        result.EndDate,
        result.Version,
        result.CreatedAtUtc,
        result.UpdatedAtUtc);
}