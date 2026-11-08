using Microsoft.EntityFrameworkCore;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Infrastructure.Persistence.Repositories;

//Carica sempre l'aggregate completo (utente + refresh token) con due query separate (AsSplitQuery): niente join che moltiplica le righe. I token sono pochi grazie alla pulizia dei token scaduti.
internal sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken) =>
        Aggregates().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken) =>
        Aggregates().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken) =>
        Aggregates().FirstOrDefaultAsync(u => u.RefreshTokens.Any(t => t.TokenHash == refreshTokenHash), cancellationToken);

    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);

    public void Remove(User user) => dbContext.Users.Remove(user);

    private IQueryable<User> Aggregates() =>
        dbContext.Users
            .Include(u => u.RefreshTokens)
            .AsSplitQuery();
}