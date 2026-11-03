using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.BuildingBlocks.Persistence.Exceptions;

//traduce  un' 🔥Optimistic concurrency: un'altra richiesta ha modificato lo stesso aggregate tra la lettura e il salvataggio (es. due refresh simultanei dello stesso token) in error api 409 (il client può ricaricare e riprovare)

public sealed class ConcurrencyConflictException(Exception innerException)
    : ConflictException("concurrency_conflict", "The resource was modified by another request. Please retry.")  //passi alla base il messaggio di errore che sarà serializzato in json e restituito al client con status code 409
{
    public Exception OriginalException { get; } = innerException;

}