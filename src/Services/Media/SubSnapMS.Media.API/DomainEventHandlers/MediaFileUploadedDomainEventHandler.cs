using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Media;
using SubSnapMS.Media.API.Infrastructure.Storage;
using SubSnapMS.Media.Core.MediaFiles;
using SubSnapMS.Media.Core.MediaFiles.Events;

namespace SubSnapMS.Media.API.DomainEventHandlers;

internal sealed class MediaFileUploadedDomainEventHandler(IIntegrationEventOutbox outbox, IObjectStorage storage)
    : IDomainEventHandler<MediaFileUploadedDomainEvent>
{
    public Task HandleAsync(MediaFileUploadedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        outbox.Add(new MediaUploaded
        {
            MediaId = domainEvent.MediaFileId.Value,
            UserId = domainEvent.UserId.Value,
            ObjectKey = domainEvent.ObjectKey,
            PublicUrl = storage.GetPublicUrl(domainEvent.ObjectKey),
            ContentType = domainEvent.ContentType,
            SizeBytes = domainEvent.SizeBytes,
            Purpose = domainEvent.Purpose.ToCode(),
            UploadedOnUtc = domainEvent.OccurredOnUtc,
            OccurredOnUtc = domainEvent.OccurredOnUtc
        });

        return Task.CompletedTask;
    }
}