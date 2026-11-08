using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Common;


public readonly record struct UserId(Guid Value)
{
    public static UserId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException("user_id.empty", "User id cannot be empty.")
            : new UserId(value);

    public override string ToString() => Value.ToString();

}