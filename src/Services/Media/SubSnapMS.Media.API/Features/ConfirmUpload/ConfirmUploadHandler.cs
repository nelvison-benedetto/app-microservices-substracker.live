using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.Contracts.Media;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.API.Infrastructure.Storage;
using SubSnapMS.Media.Core.MediaFiles;
using SubSnapMS.Media.Core.MediaFiles.Exceptions;

namespace SubSnapMS.Media.API.Features.ConfirmUpload;

/// <summary>
/// Conferma un upload verificando lo storage (non quello che dichiara il client).
/// Idempotente: confermare due volte restituisce lo stesso file.
/// Per l'avatar: il precedente viene eliminato (il file su R2 lo cancella il consumer di media.deleted.v1, dopo il commit).
/// </summary>
internal sealed class ConfirmUploadHandler(
    MediaDbContext dbContext,
    IObjectStorage storage,
    TimeProvider timeProvider) : ICommandHandler<ConfirmUploadCommand, MediaFileResponse>
{
    public async Task<MediaFileResponse> Handle(ConfirmUploadCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var file = await dbContext.MediaFiles.FirstOrDefaultAsync(f => f.Id == command.MediaFileId, cancellationToken)
                   ?? throw new MediaFileNotFoundException();

        file.EnsureOwnedBy(command.UserId);

        if (file.Status == MediaFileStatus.Uploaded)
            return MediaFileResponse.From(file, storage);

        var stored = await storage.GetMetadataAsync(file.ObjectKey, cancellationToken)
                     ?? throw new MediaUploadNotCompletedException();

        try
        {
            file.ConfirmUpload(stored.SizeBytes, stored.ContentType, now);
        }
        catch (DomainException)
        {
            // File rifiutato (troppo grande, tipo diverso, richiesta scaduta): non deve restare sullo storage
            await storage.DeleteAsync(file.ObjectKey, cancellationToken);
            throw;
        }

        if (file.Purpose == MediaPurpose.Avatar)
        {
            var previousAvatars = await dbContext.MediaFiles
                .Where(f => f.UserId == command.UserId
                            && f.Purpose == MediaPurpose.Avatar
                            && f.Status == MediaFileStatus.Uploaded
                            && f.Id != file.Id)
                .ToListAsync(cancellationToken);

            foreach (var previous in previousAvatars)
            {
                previous.Delete(now);
                dbContext.MediaFiles.Remove(previous);
            }
        }

        return MediaFileResponse.From(file, storage);
    }
}