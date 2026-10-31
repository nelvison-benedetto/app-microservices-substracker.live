using SubSnapMS.Media.API.Infrastructure.Storage;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Common;

/// <summary>Un file caricato. Url = indirizzo pubblico sul CDN (cdn.substracker.live).</summary>
public sealed record MediaFileResponse(
    Guid Id,
    string Purpose,
    string ContentType,
    long SizeBytes,
    string Url,
    DateTimeOffset UploadedAt)
{
    public static MediaFileResponse From(MediaFile file, IObjectStorage storage) => new(
        file.Id.Value,
        file.Purpose.ToCode(),
        file.ContentType,
        file.SizeBytes,
        storage.GetPublicUrl(file.ObjectKey),
        file.UploadedAtUtc ?? file.CreatedAtUtc);
}