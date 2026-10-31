using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Application.Common.Sessions;
using SubSnapMS.Identity.Core.Users;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Application.Features.Auth.Login;

internal sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    SessionTokenIssuer sessions,
    TimeProvider timeProvider) : ICommandHandler<LoginCommand, AuthTokensResult>
{
    public async Task<AuthTokensResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var email = Email.Create(command.Email);

        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            passwordHasher.VerifyDummy(command.Password);   // stessi tempi di risposta di un utente esistente
            throw new InvalidCredentialsException();
        }

        var verification = passwordHasher.Verify(user.PasswordHash.Value, command.Password);
        if (verification == PasswordVerification.Failed)
            throw new InvalidCredentialsException();

        // Solo DOPO aver verificato la password: non si rivela lo stato di un account a chi non la conosce
        user.EnsureActive();

        if (verification == PasswordVerification.SuccessRehashNeeded)
            user.UpgradePasswordHash(PasswordHash.Create(passwordHasher.Hash(command.Password)), now);

        return sessions.StartSession(user, now);
    }
}