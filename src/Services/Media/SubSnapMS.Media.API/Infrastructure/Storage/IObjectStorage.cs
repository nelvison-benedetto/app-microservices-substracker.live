namespace SubSnapMS.Media.API.Infrastructure.Storage;

/// <summary>Metadati reali di un oggetto, letti dallo storage (non dichiarati dal client).</summary>
public sealed record StoredObjectMetadata(long SizeBytes, string? ContentType);

/// <summary>Storage compatibile S3 (Cloudflare R2 in produzione, MinIO in sviluppo).</summary>
public interface IObjectStorage
{
    /// <summary>URL di PUT firmato. Firma locale (nessuna chiamata di rete). Il Content-Type fa parte della firma.</summary>
    string CreateUploadUrl(string objectKey, string contentType, DateTimeOffset expiresAtUtc);

    /// <summary>HEAD dell'oggetto. Null se non esiste (upload non avvenuto).</summary>
    Task<StoredObjectMetadata?> GetMetadataAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>Idempotente: cancellare un oggetto inesistente non è un errore.</summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);

    /// <summary>URL pubblico (CDN) da cui il browser legge il file.</summary>
    string GetPublicUrl(string objectKey);
}