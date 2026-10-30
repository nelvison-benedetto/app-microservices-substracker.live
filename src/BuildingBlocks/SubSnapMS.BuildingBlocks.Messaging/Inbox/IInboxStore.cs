using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

/// <summary>Garantisce che ogni handler processi ogni messaggio una sola volta.</summary>
public interface IInboxStore
{
    /// <summary>
    /// Esegue <paramref name="handle"/> solo se (messageId, handlerName) non è già stato processato,
    /// e registra l'esecuzione. Restituisce false se il messaggio era già stato processato (handler non eseguito).
    /// </summary>
    Task<bool> ExecuteOnceAsync(
        Guid messageId,
        string handlerName,
        Func<CancellationToken, Task> handle,
        CancellationToken cancellationToken);
}