namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

//Garantisce che ogni handler processi ogni messaggio una sola volta
public interface IInboxStore
{
    //Esegue   solo se (messageId, handlerName) non è già stato processato, e registra l'esecuzione. Restituisce false se il messaggio era già stato processato (handler non eseguito).
    Task<bool> ExecuteOnceAsync(
        Guid messageId,
        string handlerName,
        Func<CancellationToken, Task> handle,
        CancellationToken cancellationToken);
}