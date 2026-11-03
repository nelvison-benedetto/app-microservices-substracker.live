using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Media;

//upload confermato: il file è già su R2 e i metadati sono salvati in Media.
//e.g. consumer: generazione miniature, Identity se un giorno tenesse l'URL dell'avatar nel profilo.

[IntegrationEvent(Exchanges.Media, "media.uploaded.v1")]
public sealed record MediaUploaded : IntegrationEvent
{
    public required Guid MediaId { get; init; }

    public required Guid UserId { get; init; }

    public required string ObjectKey { get; init; }  //key dell'obj su R2 e.g."avatars/{guid}-foto.png"

    public required string PublicUrl { get; init; }  //url pubblico sul CDN

    public required string ContentType { get; init; }

    public required long SizeBytes { get; init; }

    public required string Purpose { get; init; }

    public required DateTimeOffset UploadedOnUtc { get; init; }
}