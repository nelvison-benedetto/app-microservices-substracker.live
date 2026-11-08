using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions;

public enum BillingCycle
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Biannual = 4,
    Yearly = 5
}

//Conversione enum ↔ codice neutro. I codici coincidono con Contracts.BillingCycles e con l'API; il frontend li traduce con next-intl. Il parsing ignora maiuscole/minuscole ("Monthly" = "monthly")
public static class BillingCycleExtensions
{
    private static readonly Dictionary<string, BillingCycle> ByCode = new(StringComparer.OrdinalIgnoreCase)
    {
        ["weekly"] = BillingCycle.Weekly,
        ["monthly"] = BillingCycle.Monthly,
        ["quarterly"] = BillingCycle.Quarterly,
        ["biannual"] = BillingCycle.Biannual,
        ["yearly"] = BillingCycle.Yearly
    };

    public static IReadOnlyList<string> AllCodes { get; } = ["weekly", "monthly", "quarterly", "biannual", "yearly"];

    public static string ToCode(this BillingCycle billingCycle) => billingCycle switch
    {
        BillingCycle.Weekly => "weekly",
        BillingCycle.Monthly => "monthly",
        BillingCycle.Quarterly => "quarterly",
        BillingCycle.Biannual => "biannual",
        BillingCycle.Yearly => "yearly",
        _ => throw new ArgumentOutOfRangeException(nameof(billingCycle), billingCycle, "Unknown billing cycle.")
    };

    public static bool IsValidCode(string? code) =>
        code is not null && ByCode.ContainsKey(code.Trim());

    public static BillingCycle Parse(string? code) =>
        code is not null && ByCode.TryGetValue(code.Trim(), out var billingCycle)
            ? billingCycle
            : throw new DomainException(
                "billing_cycle.invalid",
                $"Billing cycle must be one of: {string.Join(", ", AllCodes)}.");

}