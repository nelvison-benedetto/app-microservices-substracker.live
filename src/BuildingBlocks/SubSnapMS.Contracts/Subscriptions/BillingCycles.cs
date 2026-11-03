namespace SubSnapMS.Contracts.Subscriptions;

//codici neutri (non tradotti) del ciclo di fatturazione: la UI frontend li traduce con next-intl nella ligua dell'utente
//!Stringhe e non enum: un consumer vecchio non va in errore se ne arriva uno nuovo!
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