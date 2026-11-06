namespace SubSnapMS.BuildingBlocks.Messaging.Inbox;

//questa interface definisce cosa deve fare (“esegui questo lavoro UNA VOLTA SOLA”) senza dire come. 
public interface IInboxStore
{
    //Esegue handle solo se (messageId, handlerName) non è già stato processato, e registra l'esecuzione. Restituisce false se il messaggio era già stato processato (handler non eseguito).
    Task<bool> ExecuteOnceAsync(
        Guid messageId,
        string handlerName,
        Func<CancellationToken, Task> handle,
        CancellationToken cancellationToken);
}