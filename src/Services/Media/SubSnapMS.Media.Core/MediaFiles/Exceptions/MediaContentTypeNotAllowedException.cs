using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

public sealed class MediaContentTypeNotAllowedException(IEnumerable<string> allowedContentTypes)
    : DomainException(
        "media.content_type_not_allowed",
        $"Allowed types: {string.Join(", ", allowedContentTypes)}.");