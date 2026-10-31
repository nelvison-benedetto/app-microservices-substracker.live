using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Application.Common.Security;
using SubSnapMS.Identity.Application.Common.Sessions;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Application.Features.Users.ChangePassword;

internal sealed class ChangePasswordHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    SessionTokenIssuer sessions,
    TimeProvider timeProvider) : ICommandHandler<ChangePasswordCommand, AuthTokensResult>
{
    public async Task<AuthTokensResult> Handle(ChangePasswordCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var user = await users.GetByIdAsync(command.UserId, cancellationToken)
                   ?? throw new UserNotFoundException();

        CurrentPasswordGuard.Ensure(passwordHasher, user, command.CurrentPassword);

        // Revoca tutte le sessioni (chi conosceva la vecchia password perde l'accesso)...
        user.ChangePassword(PasswordHash.Create(passwordHasher.Hash(command.NewPassword)), now);

        // ...e ne apre una nuova per il dispositivo che ha fatto il cambio
        return sessions.StartSession(user, now);
    }
}