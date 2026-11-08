using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Application.Common.Errors;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Common.Security;

//Re-autenticazione per le operazioni sensibili (cambio email/password, cancellazione account)
internal static class CurrentPasswordGuard
{
    public static void Ensure(IPasswordHasher passwordHasher, User user, string currentPassword)
    {
        if (passwordHasher.Verify(user.PasswordHash.Value, currentPassword) == PasswordVerification.Failed)
            throw new IncorrectPasswordException();
    }
}