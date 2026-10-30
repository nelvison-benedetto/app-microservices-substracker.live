using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Subscriptions;

/// <summary>
/// Valori ammessi per BillingCycle negli eventi. Stringhe e non enum:
/// un consumer vecchio che riceve un valore nuovo non va in errore di deserializzazione,
/// può semplicemente ignorarlo o trattarlo come sconosciuto.
/// </summary>
public static class BillingCycles
{
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";
    public const string Quarterly = "quarterly";
    public const string Biannual = "biannual";
    public const string Yearly = "yearly";
}
