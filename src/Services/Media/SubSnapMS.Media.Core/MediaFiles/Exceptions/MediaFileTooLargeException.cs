using SubSnapMS.BuildingBlocks.Domain.Exceptions;

namespace SubSnapMS.Media.Core.MediaFiles.Exceptions;

/// <summary>File oltre il massimo consentito per il purpose → 400.</summary>
public class MediaFileTooLargeException : DomainException
{
    public MediaFileTooLargeException(long maxSizeBytes)
        : base("media.too_large", $"File cannot exceed {maxSizeBytes / (1024 * 1024)} MB.")
    {
        MaxSizeBytes = maxSizeBytes;
    }

    private MediaFileTooLargeException(string code, string message) : base(code, message) { }

    public long MaxSizeBytes { get; }

    /// <summary>File vuoto (0 byte) o dimensione negativa.</summary>
    public sealed class EmptyFile() : MediaFileTooLargeException("media.size_invalid", "File size must be greater than zero.");
}