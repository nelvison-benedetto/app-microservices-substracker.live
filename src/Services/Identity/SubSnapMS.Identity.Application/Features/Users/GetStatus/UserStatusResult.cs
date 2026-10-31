namespace SubSnapMS.Identity.Application.Features.Users.GetStatus;

public sealed record UserStatusResult(bool Exists, bool IsActive, bool PendingDeletion)
{
    public static readonly UserStatusResult NotFound = new(false, false, false);
}