using System.Globalization;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions;

//Tutto ciò che l'utente può modificare di un abbonamento, validato e normalizzato.
//Immutabile: una modifica crea un nuovo SubscriptionDetails, confrontato con il precedente.
public sealed record SubscriptionDetails
{
    public const int NameMaxLength = 100;
    public const int CategoryMaxLength = 50;

    private SubscriptionDetails() { }   // per EF Core

    public string Name { get; private set; } = default!;
    public string? Category { get; private set; }
    public Money Price { get; private set; } = default!;
    public BillingCycle BillingCycle { get; private set; }
    public SubscriptionPeriod Period { get; private set; } = default!;

    public static SubscriptionDetails Create(
        string name,
        string? category,
        Money price,
        BillingCycle billingCycle,
        SubscriptionPeriod period)
    {
        ArgumentNullException.ThrowIfNull(price);
        ArgumentNullException.ThrowIfNull(period);

        var normalizedName = name?.Trim();
        if (string.IsNullOrEmpty(normalizedName))
            throw new DomainException("subscription.name_required", "Name is required.");
        if (normalizedName.Length > NameMaxLength)
            throw new DomainException("subscription.name_too_long", $"Name cannot exceed {NameMaxLength} characters.");

        var normalizedCategory = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        if (normalizedCategory is { Length: > CategoryMaxLength })
            throw new DomainException("subscription.category_too_long", $"Category cannot exceed {CategoryMaxLength} characters.");

        if (!Enum.IsDefined(billingCycle))
            throw new DomainException("billing_cycle.invalid", "Billing cycle is invalid.");

        return new SubscriptionDetails
        {
            Name = normalizedName,
            Category = normalizedCategory,
            Price = price,
            BillingCycle = billingCycle,
            Period = period
        };
    }

    /// <summary>Tutti i campi come "nuovi valori" (storico della creazione).</summary>
    internal IReadOnlyList<SubscriptionFieldChange> DescribeAll() =>
    [
        new(SubscriptionFieldChange.Fields.Name, null, Name),
        new(SubscriptionFieldChange.Fields.Category, null, Category),
        new(SubscriptionFieldChange.Fields.Amount, null, Format(Price.Amount)),
        new(SubscriptionFieldChange.Fields.Currency, null, Price.Currency),
        new(SubscriptionFieldChange.Fields.BillingCycle, null, BillingCycle.ToCode()),
        new(SubscriptionFieldChange.Fields.StartDate, null, Format(Period.StartDate)),
        new(SubscriptionFieldChange.Fields.EndDate, null, Format(Period.EndDate))
    ];

    /// <summary>Solo i campi diversi rispetto a <paramref name="previous"/> (storico della modifica).</summary>
    internal IReadOnlyList<SubscriptionFieldChange> DiffFrom(SubscriptionDetails previous)
    {
        var changes = new List<SubscriptionFieldChange>();

        AddIfChanged(changes, SubscriptionFieldChange.Fields.Name, previous.Name, Name);
        AddIfChanged(changes, SubscriptionFieldChange.Fields.Category, previous.Category, Category);
        AddIfChanged(changes, SubscriptionFieldChange.Fields.Amount, Format(previous.Price.Amount), Format(Price.Amount));
        AddIfChanged(changes, SubscriptionFieldChange.Fields.Currency, previous.Price.Currency, Price.Currency);
        AddIfChanged(changes, SubscriptionFieldChange.Fields.BillingCycle, previous.BillingCycle.ToCode(), BillingCycle.ToCode());
        AddIfChanged(changes, SubscriptionFieldChange.Fields.StartDate, Format(previous.Period.StartDate), Format(Period.StartDate));
        AddIfChanged(changes, SubscriptionFieldChange.Fields.EndDate, Format(previous.Period.EndDate), Format(Period.EndDate));

        return changes;
    }

    private static void AddIfChanged(List<SubscriptionFieldChange> changes, string field, string? oldValue, string? newValue)
    {
        if (!string.Equals(oldValue, newValue, StringComparison.Ordinal))
            changes.Add(new SubscriptionFieldChange(field, oldValue, newValue));
    }

    // Formato invariante: lo storico non dipende dalla lingua del server
    private static string Format(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    private static string Format(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? Format(DateOnly? date) => date.HasValue ? Format(date.Value) : null;
}