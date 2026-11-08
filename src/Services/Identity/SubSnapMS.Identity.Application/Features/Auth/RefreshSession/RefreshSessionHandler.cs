using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Application.Common.Errors;
using SubSnapMS.Identity.Application.Common.Sessions;

namespace SubSnapMS.Identity.Application.Features.Auth.RefreshSession;

internal sealed class RefreshSessionHandler(
    IUserRepository users,
    IRefreshTokenFactory refreshTokenFactory,
    SessionTokenIssuer sessions,
    TimeProvider timeProvider) : ICommandHandler<RefreshSessionCommand, RefreshSessionResult>
{
    public async Task<RefreshSessionResult> Handle(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        var presentedHash = refreshTokenFactory.ComputeHash(command.RefreshToken);

        //Token sconosciuto: nessuno stato da salvare, quindi un'eccezione va bene
        var user = await users.GetByRefreshTokenHashAsync(presentedHash, cancellationToken)
                   ?? throw new InvalidRefreshTokenException();

        //Esito come valore: con la reuse detection la revoca della famiglia viene salvata anche se il refresh fallisce
        return sessions.RotateSession(user, presentedHash, timeProvider.GetUtcNow());
    }
}