using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users.Exceptions;

/// <summary>
/// Stesso messaggio sia per email inesistente sia per password errata:
/// non rivela quali email sono registrate (protezione da user enumeration).
/// </summary>
public sealed class InvalidCredentialsException()
    : UnauthorizedException("auth.invalid_credentials", "Invalid email or password.");