using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Contracts.UserDeletion;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.IntegrationEventHandlers;

//Saga di cancellazione account: elimina tutti i file dell'utente (ogni Delete pubblica media.deleted.v1 → il file su R2 viene cancellato dopo il commit) e conferma a Identity con UserDataDeleted.
//Dentro la transazione dell'Inbox: metadati rimossi + eventi + conferma, tutto o niente.
public sealed class UserDeletionRequestedHandler(
    MediaDbContext dbContext,
    IIntegrationEventOutbox outbox,
    TimeProvider timeProvider,
    ILogger<UserDeletionRequestedHandler> logger) : IIntegrationEventHandler<UserDeletionRequested>
{
    public async Task HandleAsync(UserDeletionRequested integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var userId = UserId.From(integrationEvent.UserId);

        var files = await dbContext.MediaFiles
            .Where(f => f.UserId == userId)
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            file.Delete(now);
            dbContext.MediaFiles.Remove(file);
        }

        outbox.Add(new UserDataDeleted
        {
            UserId = integrationEvent.UserId,
            Service = ServiceNames.Media,
            DeletedOnUtc = now
        });

        logger.LogInformation("User deletion {UserId}: {Count} media files removed", userId, files.Count);
    }
}