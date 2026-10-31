using SubSnapMS.BuildingBlocks.Application.Messaging;

namespace SubSnapMS.Identity.Application.Features.Users.Register;

public sealed record RegisterUserCommand(string Email, string Password) : ICommand<RegisterUserResult>
{
    public override string ToString() => $"RegisterUserCommand {{ Email = {Email} }}";
}