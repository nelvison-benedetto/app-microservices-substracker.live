using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Common.Sessions;

namespace SubSnapMS.Identity.Application.Features.Auth.RefreshSession;

public sealed record RefreshSessionCommand(string RefreshToken) : ICommand<RefreshSessionResult>
{
    public override string ToString() => "RefreshSessionCommand { }";
}