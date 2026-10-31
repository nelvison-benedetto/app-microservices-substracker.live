using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.Identity.Core.Users.Events;

public sealed record UserRegisteredDomainEvent(UserId UserId, Email Email, DateTimeOffset OccurredOnUtc) : IDomainEvent;