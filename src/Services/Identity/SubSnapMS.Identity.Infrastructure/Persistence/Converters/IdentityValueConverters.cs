using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SubSnapMS.Identity.Core.RefreshTokens;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Infrastructure.Persistence.Converters;

//Value Object ↔ tipo primitivo del database. Il dominio non sa nulla di come viene salvato
internal static class IdentityValueConverters
{
    public static readonly ValueConverter<UserId, Guid> UserIdConverter =
        new(id => id.Value, value => new UserId(value));

    public static readonly ValueConverter<RefreshTokenId, Guid> RefreshTokenIdConverter =
        new(id => id.Value, value => new RefreshTokenId(value));

    public static readonly ValueConverter<Email, string> EmailConverter =
        new(email => email.Value, value => Email.Create(value));

    public static readonly ValueConverter<PasswordHash, string> PasswordHashConverter =
        new(hash => hash.Value, value => PasswordHash.Create(value));
}