using SubSnapMS.Identity.Application.Features.Users.GetProfile;
using SubSnapMS.Identity.Application.Features.Users.GetStatus;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Abstractions.Queries;

//Lato lettura (CQRS): proiezioni dirette in DTO, senza tracking e senza caricare l'aggregate.
public interface IUserQueries
{
    Task<UserProfileResult?> GetProfileAsync(UserId userId, CancellationToken cancellationToken);

    Task<UserStatusResult?> GetStatusAsync(UserId userId, CancellationToken cancellationToken);
}