using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Media;
using SubSnapMS.Media.Core.MediaFiles;
using SubSnapMS.Media.Core.MediaFiles.Events;

namespace SubSnapMS.Media.API.DomainEventHandlers;

/// <summary>
/// Pubblica media.deleted.v1. Lo consuma anche Media stesso (MediaDeletedHandler) per cancellare
/// il file da R2: così la cancellazione dallo storage avviene solo DOPO il commit, con retry e DLQ.
/// </summary>
internal sealed class MediaFileDeletedDomainEventHandler(IIntegrationEventOutbox outbox)
    : IDomainEventHandler<MediaFileDeletedDomainEvent>
{
    public Task HandleAsync(MediaFileDeletedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new MediaDeleted
        {
            MediaId = domainEvent.MediaFileId.Value,
            UserId = domainEvent.UserId.Value,
            ObjectKey = domainEvent.ObjectKey,
            Purpose = domainEvent.Purpose.ToCode(),
            DeletedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}