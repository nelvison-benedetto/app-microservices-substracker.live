using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users;

/// <summary>Identità dell'utente. Generata nel dominio (Guid v7: ordinato nel tempo, indici compatti).</summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.CreateVersion7());

    /// <summary>Da un Guid ricevuto dall'esterno (claim "sub", route, evento): rifiuta Guid.Empty.</summary>
    public static UserId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException("user_id.empty", "User id cannot be empty.")
            : new UserId(value);

    public override string ToString() => Value.ToString();
}