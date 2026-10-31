using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions.Exceptions;

/// <summary>Inesistente O di un altro utente: stessa risposta (404), non si rivela l'esistenza.</summary>
public sealed class SubscriptionNotFoundException()
    : NotFoundException("subscription.not_found", "Subscription not found.");