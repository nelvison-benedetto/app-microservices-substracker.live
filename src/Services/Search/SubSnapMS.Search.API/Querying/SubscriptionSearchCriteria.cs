using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Search.API.Querying;

public enum SubscriptionStatusFilter
{
    All,
    Active,
    Ended
}

public enum SubscriptionSort
{
    Relevance,
    Name,
    Amount,
    StartDate
}

//Parametri di ricerca validati. Create lancia DomainException (400) con un codice per ogni errore
public sealed record SubscriptionSearchCriteria(
    string? Text,
    string? Category,
    string? BillingCycle,
    string? Currency,
    SubscriptionStatusFilter Status,
    decimal? MinMonthlyAmount,
    decimal? MaxMonthlyAmount,
    SubscriptionSort Sort,
    int Page,
    int PageSize)
{
    public const int MaxTextLength = 100;
    public const int MaxPageSize = 50;
    public const int DefaultPageSize = 20;

    //Limite di Elasticsearch (index.max_result_window) per from + size
    private const int MaxResultWindow = 10_000;

    public int From => (Page - 1) * PageSize;

    public static SubscriptionSearchCriteria Create(
        string? text,
        string? category,
        string? billingCycle,
        string? currency,
        string? status,
        decimal? minMonthlyAmount,
        decimal? maxMonthlyAmount,
        string? sort,
        int? page,
        int? pageSize)
    {
        text = Normalize(text);
        if (text is { Length: > MaxTextLength })
            throw new DomainException("search.query_too_long", $"Search text cannot exceed {MaxTextLength} characters.");

        billingCycle = Normalize(billingCycle)?.ToLowerInvariant();
        if (billingCycle is not null && !BillingCycles.IsKnown(billingCycle))
            throw new DomainException("search.billing_cycle_invalid", $"billingCycle must be one of: {string.Join(", ", BillingCycles.All)}.");

        currency = Normalize(currency)?.ToUpperInvariant();
        if (currency is not null && (currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper)))
            throw new DomainException("search.currency_invalid", "currency must be an ISO 4217 code (e.g. EUR).");

        var statusFilter = ParseEnum(status, SubscriptionStatusFilter.All, "search.status_invalid", "status must be one of: all, active, ended.");
        var sortOrder = ParseEnum(sort, SubscriptionSort.Relevance, "search.sort_invalid", "sort must be one of: relevance, name, amount, startDate.");

        if (minMonthlyAmount < 0 || maxMonthlyAmount < 0 || minMonthlyAmount > maxMonthlyAmount)
            throw new DomainException("search.amount_range_invalid", "Amount range is invalid.");

        var pageNumber = page ?? 1;
        var size = pageSize ?? DefaultPageSize;

        if (pageNumber < 1)
            throw new DomainException("search.page_invalid", "page must be 1 or greater.");

        if (size is < 1 or > MaxPageSize)
            throw new DomainException("search.page_size_invalid", $"pageSize must be between 1 and {MaxPageSize}.");

        if ((long)pageNumber * size > MaxResultWindow)
            throw new DomainException("search.page_too_deep", "Refine the search: too many pages requested.");

        return new SubscriptionSearchCriteria(
            text, Normalize(category), billingCycle, currency, statusFilter,
            minMonthlyAmount, maxMonthlyAmount, sortOrder, pageNumber, size);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TEnum ParseEnum<TEnum>(string? value, TEnum defaultValue, string code, string message) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return Enum.TryParse<TEnum>(value.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new DomainException(code, message);
    }
}