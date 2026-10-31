using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

/// <summary>Conferma chiesta ma il file non è (ancora) sullo storage → 409. Il client può riprovare.</summary>
public sealed class MediaUploadNotCompletedException()
    : ConflictException("media.upload_not_completed", "The file has not been uploaded yet.");