using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Auth.Logout;

/// <summary>Chiude la sessione del refresh token indicato. Idempotente.</summary>
public sealed record LogoutCommand(UserId UserId, string RefreshToken) : ICommand
{
    public override string ToString() => $"LogoutCommand {{ UserId = {UserId} }}";
}