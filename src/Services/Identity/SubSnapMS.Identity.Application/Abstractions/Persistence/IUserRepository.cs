using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Abstractions.Persistence;

/// <summary>Repository dell'aggregate User: carica SEMPRE l'aggregate completo (con i refresh token).</summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    /// <summary>Lookup per uguaglianza sull'hash SHA-256 (indice univoco).</summary>
    Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}