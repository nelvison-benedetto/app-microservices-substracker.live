using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users;

//Hash della password (mai la password in chiaro). L'algoritmo è in Infrastructure: il dominio sa solo che un hash esiste ed è valorizzato.
public sealed record PasswordHash
{
    public const int MaxLength = 512;

    private PasswordHash(string value) => Value = value;

    public string Value { get; }

    public static PasswordHash Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("password_hash.required", "Password hash is required.");

        if (value.Length > MaxLength)
            throw new DomainException("password_hash.too_long", "Password hash is too long.");

        return new PasswordHash(value);
    }

    //Non finisce mai in un log per errore
    public override string ToString() => "PasswordHash(***)";
}