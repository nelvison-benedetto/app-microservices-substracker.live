using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Media.Core.Common;
using SubSnapMS.Media.Core.MediaFiles.Events;
using SubSnapMS.Media.Core.MediaFiles.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles;

//Metadati di un file caricato da un utente. Il file vero sta sullo storage (R2), sotto ObjectKey.
//Ciclo di vita: RequestUpload (Pending) → ConfirmUpload (Uploaded) → Delete.
public sealed class MediaFile : AggregateRoot<MediaFileId>
{
    private bool _deleted;

    private MediaFile() { }   // per EF Core

    public UserId UserId { get; private set; }
    public MediaPurpose Purpose { get; private set; }

    //Normalizzato (es. "image/png"). È anche il Content-Type firmato nel presigned URL
    public string ContentType { get; private set; } = default!;

    //Pending: dimensione dichiarata dal client. Uploaded: dimensione reale letta dallo storage
    public long SizeBytes { get; private set; }

    public string ObjectKey { get; private set; } = default!;
    public MediaFileStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    //Scadenza del presigned URL
    public DateTimeOffset UploadExpiresAtUtc { get; private set; }

    //Null finché il file non è confermato
    public DateTimeOffset? UploadedAtUtc { get; private set; }

    // Ciclo di vita

    //Registra una richiesta di upload. Nessun evento: un file Pending non esiste per il resto della piattaforma.
    public static MediaFile RequestUpload(
        UserId userId,
        MediaPurpose purpose,
        string contentType,
        long declaredSizeBytes,
        DateTimeOffset now,
        TimeSpan uploadWindow)  //validità del presigned URL
    {
        if (uploadWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(uploadWindow), uploadWindow, "Upload window must be positive.");

        var normalizedContentType = MediaUploadPolicy.NormalizeContentType(contentType);
        MediaUploadPolicy.EnsureContentTypeAllowed(purpose, normalizedContentType);
        MediaUploadPolicy.EnsureSizeAllowed(purpose, declaredSizeBytes);

        var id = MediaFileId.New();

        return new MediaFile
        {
            Id = id,
            UserId = userId,
            Purpose = purpose,
            ContentType = normalizedContentType,
            SizeBytes = declaredSizeBytes,
            ObjectKey = MediaUploadPolicy.BuildObjectKey(purpose, userId, id, normalizedContentType),
            Status = MediaFileStatus.Pending,
            CreatedAtUtc = now,
            UploadExpiresAtUtc = now + uploadWindow
        };
    }

    //Conferma l'upload con i valori REALI letti dallo storage (HEAD), non quelli dichiarati dal client.
    //Restituisce false se era già confermato (idempotente: nessun evento).

    public bool ConfirmUpload(long actualSizeBytes, string? actualContentType, DateTimeOffset now)
    {
        EnsureNotDeleted();

        if (Status == MediaFileStatus.Uploaded)
            return false;

        if (now > UploadExpiresAtUtc + MediaUploadPolicy.ConfirmGracePeriod)
            throw new MediaUploadExpiredException();

        // Il Content-Type fa parte della firma: se lo storage non lo riporta, vale quello firmato
        var storedContentType = MediaUploadPolicy.NormalizeContentType(actualContentType);
        if (storedContentType.Length > 0 && storedContentType != ContentType)
            throw new MediaContentTypeMismatchException();

        MediaUploadPolicy.EnsureSizeAllowed(Purpose, actualSizeBytes);

        SizeBytes = actualSizeBytes;
        Status = MediaFileStatus.Uploaded;
        UploadedAtUtc = now;

        Raise(new MediaFileUploadedDomainEvent(Id, UserId, Purpose, ObjectKey, ContentType, SizeBytes, now));
        return true;
    }

    //Segna il file come eliminato (la riga la rimuove chi chiama, con dbContext.Remove).
    //L'evento viene emesso anche per un file Pending: l'oggetto può esistere su R2 senza conferma (es. saga di cancellazione account) e l'evento è ciò che lo fa cancellare dopo il commit.
    //Una seconda chiamata non ha effetto.
    public void Delete(DateTimeOffset now)
    {
        if (_deleted)
            return;

        _deleted = true;
        Raise(new MediaFileDeletedDomainEvent(Id, UserId, Purpose, ObjectKey, now));
    }

    // Regole

    //Un file di un altro utente è trattato come inesistente (404): non se ne rivela l'esistenza
    public void EnsureOwnedBy(UserId userId)
    {
        if (UserId != userId)
            throw new MediaFileNotFoundException();
    }

    private void EnsureNotDeleted()
    {
        if (_deleted)
            throw new InvalidOperationException("The media file has been deleted.");
    }
}