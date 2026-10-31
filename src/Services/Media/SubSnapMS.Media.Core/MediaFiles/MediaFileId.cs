using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles;

public readonly record struct MediaFileId(Guid Value)
{
    public static MediaFileId New() => new(Guid.CreateVersion7());

    public static MediaFileId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException("media_id.empty", "Media id cannot be empty.")
            : new MediaFileId(value);

    public override string ToString() => Value.ToString();
}