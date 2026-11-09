using SubSnapMS.Contracts.Subscriptions;

namespace SubSnapMS.Analytics.API.Calculations;

//Equivalente mensile di un prezzo. DEVE restare identico a Money.ToMonthly (Subscriptions.Core): ×52/12 · ×1 · /3 · /6 · /12, arrotondato a 2 decimali per singolo abbonamento.
//Così i totali della dashboard coincidono con la somma dei monthlyAmount della lista.
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
            _ => 0m   //ciclo introdotto da una versione più nuova di Subscriptions: escluso dai totali, non va in errore
        };

        return decimal.Round(monthly, 2, MidpointRounding.AwayFromZero);
    }
}