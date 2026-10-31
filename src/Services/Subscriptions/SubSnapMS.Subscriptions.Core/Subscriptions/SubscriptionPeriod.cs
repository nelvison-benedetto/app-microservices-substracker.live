using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions;

/// <summary>Inizio dell'abbonamento ed eventuale fine (disdetta). Fine ≥ inizio.</summary>
public sealed record SubscriptionPeriod
{
    private SubscriptionPeriod() { }   // per EF Core

    public DateOnly StartDate { get; private set; }

    /// <summary>Null = abbonamento in corso, senza data di fine.</summary>
    public DateOnly? EndDate { get; private set; }

    public static SubscriptionPeriod Create(DateOnly startDate, DateOnly? endDate)
    {
        if (startDate == default)
            throw new DomainException("subscription.start_date_required", "Start date is required.");

        if (endDate is { } end && end < startDate)
            throw new DomainException("subscription.end_before_start", "End date cannot be before the start date.");

        return new SubscriptionPeriod { StartDate = startDate, EndDate = endDate };
    }

    /// <summary>Attivo in una certa data (inizio incluso, fine inclusa).</summary>
    public bool IsActiveOn(DateOnly date) =>
        date >= StartDate && (EndDate is null || date <= EndDate.Value);
}