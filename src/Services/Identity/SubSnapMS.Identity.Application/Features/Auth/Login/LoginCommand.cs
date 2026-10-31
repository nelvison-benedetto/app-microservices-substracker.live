using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Common.Sessions;

namespace SubSnapMS.Identity.Application.Features.Auth.Login;

public sealed record LoginCommand(string Email, string Password) : ICommand<AuthTokensResult>
{
    public override string ToString() => $"LoginCommand {{ Email = {Email} }}";
}