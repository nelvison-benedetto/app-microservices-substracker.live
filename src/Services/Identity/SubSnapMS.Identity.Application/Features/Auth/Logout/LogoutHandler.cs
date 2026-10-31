using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Security;

namespace SubSnapMS.Identity.Application.Features.Auth.Logout;

internal sealed class LogoutHandler(
    IUserRepository users,
    IRefreshTokenFactory refreshTokenFactory,
    TimeProvider timeProvider) : ICommandHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
            return;   // idempotente: il frontend cancella comunque i cookie

        // Cerca solo tra i token dell'utente autenticato: non si può chiudere la sessione di un altro
        user.SignOut(refreshTokenFactory.ComputeHash(command.RefreshToken), timeProvider.GetUtcNow());
    }
}