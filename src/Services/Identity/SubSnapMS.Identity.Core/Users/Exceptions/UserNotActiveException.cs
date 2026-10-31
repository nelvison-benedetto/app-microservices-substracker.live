using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users.Exceptions;

public sealed class UserNotActiveException()
    : ForbiddenException("user.not_active", "This account is not active.");