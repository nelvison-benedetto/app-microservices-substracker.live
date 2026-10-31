using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Application.Common.Security;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Application.Features.Users.RequestDeletion;

/// <summary>
/// Marca l'utente PendingDeletion e chiude le sessioni. Il resto della saga parte al salvataggio:
/// UserDeletionRequestedDomainEventHandler crea il processo UserDeletion e pubblica l'integration event.
/// </summary>
internal sealed class RequestUserDeletionHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : ICommandHandler<RequestUserDeletionCommand>
{
    public async Task Handle(RequestUserDeletionCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken)
                   ?? throw new UserNotFoundException();

        CurrentPasswordGuard.Ensure(passwordHasher, user, command.CurrentPassword);

        user.RequestDeletion(timeProvider.GetUtcNow());
    }
}