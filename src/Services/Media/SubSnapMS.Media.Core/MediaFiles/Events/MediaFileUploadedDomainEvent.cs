using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.Core.MediaFiles.Events;

//Valori reali (dallo storage). Tradotto in media.uploaded.v1 dall'API
public sealed record MediaFileUploadedDomainEvent(
    MediaFileId MediaFileId,
    UserId UserId,
    MediaPurpose Purpose,
    string ObjectKey,
    string ContentType,
    long SizeBytes,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;