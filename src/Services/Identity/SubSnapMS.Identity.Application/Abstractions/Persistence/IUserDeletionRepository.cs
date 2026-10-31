using SubSnapMS.Identity.Core.Deletions;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Abstractions.Persistence;

public interface IUserDeletionRepository
{
    Task<UserDeletion?> GetByIdAsync(UserId userId, CancellationToken cancellationToken);

    void Add(UserDeletion deletion);
}