using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

/// <summary>Il file caricato non ha il tipo per cui era stato emesso l'URL → 400.</summary>
public sealed class MediaContentTypeMismatchException()
    : DomainException("media.content_type_mismatch", "The uploaded file type does not match the requested type.");