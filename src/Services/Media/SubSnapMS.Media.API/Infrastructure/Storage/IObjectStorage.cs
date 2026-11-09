namespace SubSnapMS.Media.API.Infrastructure.Storage;

//Metadati reali di un oggetto, letti dallo storage (non dichiarati dal client)
public sealed record StoredObjectMetadata(long SizeBytes, string? ContentType);

//Storage compatibile S3 (Cloudflare R2 in produzione, MinIO in sviluppo)
public interface IObjectStorage
{
    //URL di PUT firmato. Firma locale (nessuna chiamata di rete). Il Content-Type fa parte della firma
    string CreateUploadUrl(string objectKey, string contentType, DateTimeOffset expiresAtUtc);

    //HEAD dell'oggetto. Null se non esiste (upload non avvenuto)
    Task<StoredObjectMetadata?> GetMetadataAsync(string objectKey, CancellationToken cancellationToken);

    //Idempotente: cancellare un oggetto inesistente non è un errore
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);

    //URL pubblico (CDN) da cui il browser legge il file
    string GetPublicUrl(string objectKey);
}