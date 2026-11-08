using Microsoft.EntityFrameworkCore;
using SubSnapMS.Identity.Application.Abstractions.Queries;
using SubSnapMS.Identity.Application.Features.Users.GetProfile;
using SubSnapMS.Identity.Application.Features.Users.GetStatus;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Infrastructure.Persistence.Queries;

//Lato lettura (CQRS): proiezioni dirette in DTO, AsNoTracking, solo le colonne necessarie. Nessun refresh token caricato, nessun aggregate costruito.
internal sealed class UserQueries(IdentityDbContext dbContext) : IUserQueries
{
    public Task<UserProfileResult?> GetProfileAsync(UserId userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserProfileResult(
                u.Id.Value,
                u.Email.Value,
                u.Status.ToString(),
                u.CreatedAtUtc,
                u.UpdatedAtUtc,
                u.LastLoginAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserStatusResult?> GetStatusAsync(UserId userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new UserStatusResult(
                true,
                u.Status == UserStatus.Active,
                u.Status == UserStatus.PendingDeletion))
            .FirstOrDefaultAsync(cancellationToken);
}