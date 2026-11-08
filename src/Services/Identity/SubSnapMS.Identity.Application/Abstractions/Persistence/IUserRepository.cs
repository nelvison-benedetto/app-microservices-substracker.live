using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Application.Abstractions.Persistence;

//Repository dell'aggregate User: carica SEMPRE l'aggregate completo (con i refresh token)
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);

    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken);

    //Lookup per uguaglianza sull'hash SHA-256 (indice univoco)
    Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);

    void Add(User user);

    void Remove(User user);
}