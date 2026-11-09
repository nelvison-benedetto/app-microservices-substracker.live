using Microsoft.Extensions.Logging;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Media;
using SubSnapMS.Media.API.Infrastructure.Storage;

namespace SubSnapMS.Media.API.IntegrationEventHandlers;

//Cancella il file dallo storage quando i metadati sono già stati eliminati (evento pubblicato dopo il commit).
//Idempotente: cancellare un oggetto che non esiste più non è un errore. Errori di rete → retry, poi DLQ.
public sealed class MediaDeletedHandler(IObjectStorage storage, ILogger<MediaDeletedHandler> logger)
    : IIntegrationEventHandler<MediaDeleted>
{
    public async Task HandleAsync(MediaDeleted integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        await storage.DeleteAsync(integrationEvent.ObjectKey, cancellationToken);
        logger.LogInformation("Media {MediaId}: object {ObjectKey} deleted from storage", integrationEvent.MediaId, integrationEvent.ObjectKey);
    }
}