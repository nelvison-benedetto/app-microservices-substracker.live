using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain;

/// <summary>
/// Radice di un aggregate: unico punto d'accesso per modificare l'aggregate e i suoi figli,
/// confine di consistenza e di transazione. Ha un proprio repository; i figli no.
/// Gli aggregate si referenziano tra loro solo per Id.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Registra un fatto avvenuto. Viene gestito al salvataggio, nella stessa transazione.</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}