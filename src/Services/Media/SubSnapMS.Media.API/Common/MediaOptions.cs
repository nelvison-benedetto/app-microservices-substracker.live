namespace SubSnapMS.Media.API.Common;

/// <summary>Sezione "Media:Uploads".</summary>
public sealed class MediaUploadOptions
{
    public const string SectionName = "Media:Uploads";

    /// <summary>Validità del presigned URL (e tempo massimo per confermare l'upload).</summary>
    public TimeSpan UploadUrlLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>Dopo quanto (oltre la scadenza) una richiesta mai confermata viene eliminata.</summary>
    public TimeSpan PendingCleanupAfter { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}