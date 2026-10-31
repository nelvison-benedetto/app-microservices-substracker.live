using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Application.Common.Errors;

/// <summary>
/// Refresh non riuscito (→ 401). Il code distingue il motivo per il client:
/// auth.invalid_refresh_token, auth.refresh_token_expired, auth.refresh_token_reused, user.not_active.
/// </summary>
public sealed class InvalidRefreshTokenException(string code = "auth.invalid_refresh_token")
    : UnauthorizedException(code, "The refresh token is invalid or expired.");