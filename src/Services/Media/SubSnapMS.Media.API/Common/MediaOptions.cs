namespace SubSnapMS.Media.API.Common;

//Sezione "Media:Uploads"
public sealed class MediaUploadOptions
{
    public const string SectionName = "Media:Uploads";

    //Validità del presigned URL (e tempo massimo per confermare l'upload)
    public TimeSpan UploadUrlLifetime { get; set; } = TimeSpan.FromMinutes(5);

    //Dopo quanto (oltre la scadenza) una richiesta mai confermata viene eliminata
    public TimeSpan PendingCleanupAfter { get; set; } = TimeSpan.FromHours(1);

    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
}