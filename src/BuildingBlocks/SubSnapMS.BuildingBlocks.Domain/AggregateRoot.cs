namespace SubSnapMS.BuildingBlocks.Domain;

//ROOT di un aggregate: unico punto d'accesso per modificare l'aggregate e i suoi figli, confine di consistenza e di transazione. Ha un proprio repository, i figli no. (e.g. Subscription è root, HistorySubscription è un children e non ha repository e per essere modificato devi passare dalla root)
//gli aggregate si referenziano tra loro solo per Id.

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];  //lista degli eventi registrati e non ancora gestiti

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();  //espone gli events in sola lettura

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);  //registra un event. Viene gestito al salvataggio, nella stessa transazione.

    public void ClearDomainEvents() => _domainEvents.Clear();  //svuota la lista 

}


