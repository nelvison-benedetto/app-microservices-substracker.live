using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Graph.API.Projection;

/// <summary>
/// Equivalente mensile. DEVE restare identico a Money.ToMonthly (Subscriptions.Core) e a MonthlyCost
/// di Analytics e Search: ×52/12 · ×1 · /3 · /6 · /12, arrotondato a 2 decimali.
/// </summary>
public static class MonthlyCost
{
    public static decimal Calculate(decimal amount, string billingCycle)
    {
        var monthly = billingCycle switch
        {
            BillingCycles.Weekly => amount * 52m / 12m,
            BillingCycles.Monthly => amount,
            BillingCycles.Quarterly => amount / 3m,
            BillingCycles.Biannual => amount / 6m,
            BillingCycles.Yearly => amount / 12m,
            _ => 0m
        };

        return decimal.Round(monthly, 2, MidpointRounding.AwayFromZero);
    }
}