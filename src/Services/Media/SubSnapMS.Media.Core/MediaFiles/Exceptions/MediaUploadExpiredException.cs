using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

//Richiesta di upload scaduta → 400. Il client deve richiedere un nuovo URL
public sealed class MediaUploadExpiredException()
    : DomainException("media.upload_expired", "The upload request has expired. Request a new upload URL.");