using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Common.Sessions;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Users.ChangePassword;

//>Restituisce una nuova sessione: tutte le altre vengono chiuse
public sealed record ChangePasswordCommand(UserId UserId, string CurrentPassword, string NewPassword) : ICommand<AuthTokensResult>
{
    public override string ToString() => $"ChangePasswordCommand {{ UserId = {UserId} }}";
}