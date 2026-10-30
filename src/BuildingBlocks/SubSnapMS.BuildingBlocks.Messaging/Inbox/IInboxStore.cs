using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

/// <summary>Registro dei messaggi già processati, per handler.</summary>
public interface IInboxStore
{
    Task<bool> HasBeenProcessedAsync(Guid messageId, string handlerName, CancellationToken cancellationToken);

    /// <summary>Registra il messaggio come processato. Nell'implementazione EF salva anche le modifiche dell'handler.</summary>
    Task MarkProcessedAsync(Guid messageId, string handlerName, CancellationToken cancellationToken);
}
