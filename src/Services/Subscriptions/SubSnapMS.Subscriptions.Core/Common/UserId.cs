using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Common;

/// <summary>
/// Proprietario di un abbonamento. Tipo di QUESTO servizio (non quello di Identity):
/// i servizi condividono solo il valore (il claim "sub" del JWT), mai il codice di dominio.
/// </summary>
public readonly record struct UserId(Guid Value)
{
    public static UserId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException("user_id.empty", "User id cannot be empty.")
            : new UserId(value);

    public override string ToString() => Value.ToString();
}