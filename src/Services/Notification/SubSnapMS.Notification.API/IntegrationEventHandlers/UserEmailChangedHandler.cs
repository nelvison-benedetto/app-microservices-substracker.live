using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.IntegrationEventHandlers;

public sealed class UserEmailChangedHandler(RecipientStore recipients) : IIntegrationEventHandler<UserEmailChanged>
{
    public Task HandleAsync(UserEmailChanged integrationEvent, MessageContext context, CancellationToken cancellationToken) =>
        recipients.UpsertEmailAsync(integrationEvent.UserId, integrationEvent.NewEmail, integrationEvent.ChangedOnUtc, cancellationToken);
}