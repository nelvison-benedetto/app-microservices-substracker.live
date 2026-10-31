using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions.Exceptions;

/// <summary>Il client ha modificato una versione superata (es. due schede aperte) → 409.</summary>
public sealed class SubscriptionVersionMismatchException()
    : ConflictException(
        "subscription.version_mismatch",
        "The subscription was modified in the meantime. Reload it and try again.");