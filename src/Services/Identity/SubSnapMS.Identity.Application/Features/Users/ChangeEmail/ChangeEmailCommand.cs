using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Users.ChangeEmail;

public sealed record ChangeEmailCommand(UserId UserId, string NewEmail, string CurrentPassword) : ICommand
{
    public override string ToString() => $"ChangeEmailCommand {{ UserId = {UserId}, NewEmail = {NewEmail} }}";
}