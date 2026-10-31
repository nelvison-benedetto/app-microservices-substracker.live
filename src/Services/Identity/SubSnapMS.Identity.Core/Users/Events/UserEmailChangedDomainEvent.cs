using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.Identity.Core.Users.Events;

public sealed record UserEmailChangedDomainEvent(UserId UserId, Email NewEmail, DateTimeOffset OccurredOnUtc) : IDomainEvent;