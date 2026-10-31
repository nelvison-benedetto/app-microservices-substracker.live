namespace SubSnapMS.Subscriptions.Core.Subscriptions;

/// <summary>
/// Un campo cambiato, con i valori in formato invariante ("12.99", "2026-04-01", "monthly").
/// OldValue null = valore iniziale (creazione) o campo prima vuoto.
/// </summary>
public sealed record SubscriptionFieldChange(string Field, string? OldValue, string? NewValue)
{
    /// <summary>Nomi dei campi: gli stessi (camelCase) del JSON dell'API.</summary>
    public static class Fields
    {
        public const string Name = "name";
        public const string Category = "category";
        public const string Amount = "amount";
        public const string Currency = "currency";
        public const string BillingCycle = "billingCycle";
        public const string StartDate = "startDate";
        public const string EndDate = "endDate";
    }
}