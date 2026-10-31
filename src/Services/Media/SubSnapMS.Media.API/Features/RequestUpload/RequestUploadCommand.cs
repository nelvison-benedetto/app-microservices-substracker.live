using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Features.RequestUpload;

public sealed record RequestUploadCommand(UserId UserId, string Purpose, string ContentType, long SizeBytes)
    : ICommand<UploadUrlResponse>;

/// <param name="RequiredHeaders">Header da inviare nel PUT verso lo storage (il Content-Type è parte della firma).</param>
public sealed record UploadUrlResponse(
    Guid MediaId,
    string UploadUrl,
    string ObjectKey,
    DateTimeOffset ExpiresAt,
    IReadOnlyDictionary<string, string> RequiredHeaders);