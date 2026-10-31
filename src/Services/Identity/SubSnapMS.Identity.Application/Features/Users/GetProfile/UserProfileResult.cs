namespace SubSnapMS.Identity.Application.Features.Users.GetProfile;

public sealed record UserProfileResult(
    Guid Id,
    string Email,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? LastLoginAtUtc);