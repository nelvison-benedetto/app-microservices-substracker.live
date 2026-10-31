using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Infrastructure.Persistence.Converters;

/// <summary>Value Object ↔ tipo primitivo del database. Il dominio non sa nulla di come viene salvato.</summary>
internal static class SubscriptionsValueConverters
{
    public static readonly ValueConverter<UserId, Guid> UserIdConverter =
        new(id => id.Value, value => new UserId(value));

    public static readonly ValueConverter<SubscriptionId, Guid> SubscriptionIdConverter =
        new(id => id.Value, value => new SubscriptionId(value));

    public static readonly ValueConverter<SubscriptionHistoryEntryId, Guid> HistoryEntryIdConverter =
        new(id => id.Value, value => new SubscriptionHistoryEntryId(value));

    /// <summary>Salvato come codice neutro ("monthly"): lo stesso valore di API ed eventi.</summary>
    public static readonly ValueConverter<BillingCycle, string> BillingCycleConverter =
        new(cycle => cycle.ToCode(), code => BillingCycleExtensions.Parse(code));
}