using Microsoft.Extensions.Logging;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.Identity.Core.Users.Events;

namespace SubSnapMS.Identity.Application.DomainEventHandlers;

/// <summary>Evento di sicurezza: log a livello Warning (finisce su Sentry/log centralizzati per un alert).</summary>
internal sealed class RefreshTokenReuseDetectedDomainEventHandler(ILogger<RefreshTokenReuseDetectedDomainEventHandler> logger)
    : IDomainEventHandler<RefreshTokenReuseDetectedDomainEvent>
{
    public Task HandleAsync(RefreshTokenReuseDetectedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "SECURITY: refresh token reuse detected for user {UserId}. Session family {FamilyId} has been revoked.",
            domainEvent.UserId, domainEvent.FamilyId);

        return Task.CompletedTask;
    }
}