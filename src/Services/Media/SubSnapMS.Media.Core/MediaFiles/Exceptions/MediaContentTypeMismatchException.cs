using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

//Il file caricato non ha il tipo per cui era stato emesso l'URL → 400
public sealed class MediaContentTypeMismatchException()
    : DomainException("media.content_type_mismatch", "The uploaded file type does not match the requested type.");