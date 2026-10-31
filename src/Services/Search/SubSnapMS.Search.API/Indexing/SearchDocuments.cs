namespace SubSnapMS.Search.API.Indexing;

/// <summary>Documento nell'indice "subscriptions". I nomi (camelCase) coincidono con il mapping strict.</summary>
public sealed record SubscriptionSearchDocument(
    Guid SubscriptionId,
    Guid UserId,
    bool Deleted,
    string Name,
    string? Category,
    decimal Amount,
    decimal MonthlyAmount,
    string Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate,
    DateTimeOffset UpdatedAt);

/// <summary>Tombstone di un abbonamento cancellato: nessun dato, solo id e data (per la pulizia).</summary>
public sealed record SubscriptionTombstoneDocument(
    Guid SubscriptionId,
    Guid UserId,
    bool Deleted,
    DateTimeOffset UpdatedAt);

/// <summary>Utente cancellato: blocca gli eventi arrivati in ritardo.</summary>
public sealed record DeletedUserDocument(Guid UserId, DateTimeOffset DeletedAt);