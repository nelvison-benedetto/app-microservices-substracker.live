using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users.Exceptions;

public sealed class UserNotFoundException()
    : NotFoundException("user.not_found", "User not found.");