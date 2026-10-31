using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Features.Users.RequestDeletion;

/// <summary>Avvia la saga di cancellazione. L'endpoint risponde 202 Accepted.</summary>
public sealed record RequestUserDeletionCommand(UserId UserId, string CurrentPassword) : ICommand
{
    public override string ToString() => $"RequestUserDeletionCommand {{ UserId = {UserId} }}";
}