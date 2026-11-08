namespace SubSnapMS.Subscriptions.Core.Subscriptions;

//Un campo cambiato, con i valori in formato invariante ("12.99", "2026-04-01", "monthly"). OldValue null = valore iniziale (creazione) o campo prima vuoto.
public sealed record SubscriptionFieldChange(string Field, string? OldValue, string? NewValue)
{
    //nomi dei campi: gli stessi (camelCase) del JSON dell'API
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