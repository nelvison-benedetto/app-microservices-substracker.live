using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.Core.MediaFiles.Events;

/// <summary>
/// Tradotto in media.deleted.v1. Può riguardare un file mai confermato:
/// i consumer ignorano i MediaId di cui non hanno mai ricevuto media.uploaded.v1.
/// </summary>
public sealed record MediaFileDeletedDomainEvent(
    MediaFileId MediaFileId,
    UserId UserId,
    MediaPurpose Purpose,
    string ObjectKey,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;