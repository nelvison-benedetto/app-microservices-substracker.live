using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.Identity.Core.Users.Events;

/// <summary>Possibile furto di token: la famiglia è stata revocata. Usato per log di sicurezza e alert.</summary>
public sealed record RefreshTokenReuseDetectedDomainEvent(UserId UserId, Guid FamilyId, DateTimeOffset OccurredOnUtc) : IDomainEvent;