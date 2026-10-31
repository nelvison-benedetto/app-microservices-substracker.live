using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Infrastructure.Identity;

/// <summary>Verifica sincrona su Identity: un utente disattivato o in cancellazione non può caricare file.</summary>
public interface IUserStatusChecker
{
    /// <summary>Lancia ForbiddenException (403) se l'utente non può caricare file.</summary>
    Task EnsureCanUploadAsync(UserId userId, CancellationToken cancellationToken);
}