using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users.Exceptions;

public sealed class EmailAlreadyRegisteredException()
    : ConflictException("user.email_taken", "This email is already registered.");