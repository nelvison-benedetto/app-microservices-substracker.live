using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.Identity.Core.Users.Events;

public sealed record UserDeletionRequestedDomainEvent(UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;