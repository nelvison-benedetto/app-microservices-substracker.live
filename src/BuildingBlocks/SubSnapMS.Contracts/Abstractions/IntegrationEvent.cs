using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Abstractions;

/// <summary>
/// Base di ogni integration event scambiato tra servizi.
/// Solo dati: nessuna logica, nessuna dipendenza.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Id univoco del messaggio. Usato anche dall'Inbox per l'idempotenza.</summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
