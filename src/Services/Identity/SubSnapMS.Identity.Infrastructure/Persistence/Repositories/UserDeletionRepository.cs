using Microsoft.EntityFrameworkCore;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Core.Deletions;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserDeletionRepository(IdentityDbContext dbContext) : IUserDeletionRepository
{
    public Task<UserDeletion?> GetByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        dbContext.UserDeletions.FirstOrDefaultAsync(d => d.Id == userId, cancellationToken);

    public void Add(UserDeletion deletion) => dbContext.UserDeletions.Add(deletion);
}