using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Core.Deletions.Events;

public sealed record UserDeletionCompletedDomainEvent(UserId UserId, DateTimeOffset OccurredOnUtc) : IDomainEvent;