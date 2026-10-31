using SubSnapMS.Media.Core.Common;
using SubSnapMS.Media.Core.MediaFiles.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles;

/// <summary>
/// Regole di upload per ogni purpose: tipi ammessi, dimensione massima, formato della chiave.
/// Pubblica perché il validator dell'API la usa per rispondere subito (400) prima di toccare il dominio;
/// il dominio la riapplica comunque (in RequestUpload e, sui valori reali, in ConfirmUpload).
/// </summary>
public static class MediaUploadPolicy
{
    private const long OneMegabyte = 1024 * 1024;

    /// <summary>
    /// Oltre la scadenza del presigned URL, la conferma è accettata ancora per questo tempo:
    /// un upload iniziato poco prima della scadenza può finire dopo.
    /// </summary>
    public static readonly TimeSpan ConfirmGracePeriod = TimeSpan.FromMinutes(15);

    // Niente SVG (può contenere script, servito da un dominio pubblico) e niente GIF.
    private static readonly IReadOnlyList<string> ImageContentTypes = ["image/jpeg", "image/png", "image/webp"];

    private static readonly Dictionary<string, string> FileExtensions = new(StringComparer.Ordinal)
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp"
    };

    public static long MaxSizeBytes(MediaPurpose purpose) => purpose switch
    {
        MediaPurpose.Avatar => 5 * OneMegabyte,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unknown media purpose.")
    };

    /// <summary>Content type ammessi, in forma normalizzata (minuscolo, senza parametri).</summary>
    public static IReadOnlyList<string> AllowedContentTypes(MediaPurpose purpose) => purpose switch
    {
        MediaPurpose.Avatar => ImageContentTypes,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Unknown media purpose.")
    };

    /// <summary>"Image/PNG; charset=binary" → "image/png". Null o vuoto → stringa vuota.</summary>
    public static string NormalizeContentType(string? contentType) =>
        string.IsNullOrWhiteSpace(contentType)
            ? string.Empty
            : contentType.Split(';', 2)[0].Trim().ToLowerInvariant();

    internal static void EnsureContentTypeAllowed(MediaPurpose purpose, string normalizedContentType)
    {
        if (!AllowedContentTypes(purpose).Contains(normalizedContentType))
            throw new MediaContentTypeNotAllowedException(AllowedContentTypes(purpose));
    }

    internal static void EnsureSizeAllowed(MediaPurpose purpose, long sizeBytes)
    {
        if (sizeBytes <= 0)
            throw new MediaFileTooLargeException.EmptyFile();

        if (sizeBytes > MaxSizeBytes(purpose))
            throw new MediaFileTooLargeException(MaxSizeBytes(purpose));
    }

    /// <summary>
    /// Chiave generata dal server: nessun nome file dell'utente (niente caratteri strani,
    /// niente dati personali in un URL pubblico). Es. avatars/{userId}/{mediaId}.webp
    /// </summary>
    internal static string BuildObjectKey(MediaPurpose purpose, UserId userId, MediaFileId mediaFileId, string normalizedContentType) =>
        $"{purpose.ToStorageFolder()}/{userId.Value}/{mediaFileId.Value}.{FileExtensions[normalizedContentType]}";
}