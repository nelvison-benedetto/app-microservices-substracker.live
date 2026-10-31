using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.API.Infrastructure.Identity;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.API.Infrastructure.Storage;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.RequestUpload;

/// <summary>
/// 1. verifica via gRPC che l'utente sia attivo (non in cancellazione);
/// 2. registra la richiesta (Pending) con chiave generata dal server;
/// 3. firma un URL di PUT valido pochi minuti. La firma è locale: nessuna chiamata allo storage.
/// </summary>
internal sealed class RequestUploadHandler(
    MediaDbContext dbContext,
    IUserStatusChecker userStatus,
    IObjectStorage storage,
    IOptions<MediaUploadOptions> options,
    TimeProvider timeProvider) : ICommandHandler<RequestUploadCommand, UploadUrlResponse>
{
    public async Task<UploadUrlResponse> Handle(RequestUploadCommand command, CancellationToken cancellationToken)
    {
        await userStatus.EnsureCanUploadAsync(command.UserId, cancellationToken);

        var file = MediaFile.RequestUpload(
            command.UserId,
            MediaPurposeExtensions.Parse(command.Purpose),
            command.ContentType,
            command.SizeBytes,
            timeProvider.GetUtcNow(),
            options.Value.UploadUrlLifetime);

        dbContext.MediaFiles.Add(file);

        var uploadUrl = storage.CreateUploadUrl(file.ObjectKey, file.ContentType, file.UploadExpiresAtUtc);

        return new UploadUrlResponse(
            file.Id.Value,
            uploadUrl,
            file.ObjectKey,
            file.UploadExpiresAtUtc,
            new Dictionary<string, string> { ["Content-Type"] = file.ContentType });
    }
}