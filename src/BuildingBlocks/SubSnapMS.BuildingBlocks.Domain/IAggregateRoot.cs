using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain;

/// <summary>
/// Marcatore non generico degli aggregate root: permette alla persistenza di raccogliere
/// i domain event da tutti gli aggregate tracciati, qualunque sia il tipo del loro Id.
/// </summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}