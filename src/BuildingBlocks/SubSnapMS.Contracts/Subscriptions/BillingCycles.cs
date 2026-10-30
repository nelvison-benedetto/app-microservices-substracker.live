using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Subscriptions;

/// <summary>
/// Codici neutri (non tradotti) del ciclo di fatturazione: la UI li traduce con next-intl.
/// Stringhe e non enum: un consumer vecchio non va in errore se ne arriva uno nuovo.
/// </summary>
public static class BillingCycles
{
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";
    public const string Quarterly = "quarterly";
    public const string Biannual = "biannual";
    public const string Yearly = "yearly";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { Weekly, Monthly, Quarterly, Biannual, Yearly };

    public static bool IsKnown(string? value) => value is not null && All.Contains(value);
}