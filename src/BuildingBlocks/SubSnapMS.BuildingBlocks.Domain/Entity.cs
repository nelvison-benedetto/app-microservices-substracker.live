namespace SubSnapMS.BuildingBlocks.Domain;

//Entità con identità: 2 entità sono uguali se hanno stesso tipo e stesso Id.
//L'Id è generato nel dominio alla creazione, quindi è stabile anche prima del salvataggio.

public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull   //Entity<TId> è la base di tutto ciò che nel dominio ha un’identità: gli aggregate root (User, Subscription, MediaFile) e i loro figli (RefreshToken, SubscriptionHistoryEntry)
{
    public TId Id { get; protected set; } = default!;

    public bool Equals(Entity<TId>? other) =>
        other is not null &&
        GetType() == other.GetType() &&
        EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) => Equals(left, right);

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !Equals(left, right);


}

