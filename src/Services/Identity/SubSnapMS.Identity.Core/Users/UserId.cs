using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Identity.Core.Users;

//Identità dell'utente. Generata nel dominio (Guid v7: ordinato nel tempo, indici compatti)
public readonly record struct UserId(Guid Value)
{
    public static UserId New() => new(Guid.CreateVersion7());

    //Da un Guid ricevuto dall'esterno (claim "sub", route, evento): rifiuta Guid.Empty
    public static UserId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException("user_id.empty", "User id cannot be empty.")
            : new UserId(value);

    public override string ToString() => Value.ToString();
}