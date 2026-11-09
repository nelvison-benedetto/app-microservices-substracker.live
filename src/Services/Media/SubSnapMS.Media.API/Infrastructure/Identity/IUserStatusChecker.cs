using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Infrastructure.Identity;

//Verifica sincrona su Identity: un utente disattivato o in cancellazione non può caricare file
public interface IUserStatusChecker
{
    //Lancia ForbiddenException (403) se l'utente non può caricare file
    Task EnsureCanUploadAsync(UserId userId, CancellationToken cancellationToken);
}