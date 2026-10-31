using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.BuildingBlocks.Persistence.Exceptions;

/// <summary>
/// Optimistic concurrency: un'altra richiesta ha modificato lo stesso aggregate tra la lettura e il salvataggio
/// (es. due refresh simultanei dello stesso token). → 409: il client può ricaricare e riprovare.
/// </summary>
public sealed class ConcurrencyConflictException(Exception innerException)
    : ConflictException("concurrency_conflict", "The resource was modified by another request. Please retry.")
{
    public Exception OriginalException { get; } = innerException;
}