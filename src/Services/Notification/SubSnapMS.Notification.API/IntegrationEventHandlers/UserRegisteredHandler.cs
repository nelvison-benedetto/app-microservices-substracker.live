using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Notification.API.Configuration;
using SubSnapMS.Notification.API.Delivery;
using SubSnapMS.Notification.API.Email;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.IntegrationEventHandlers;

//Salva l'email e invia il benvenuto (una volta sola, solo per registrazioni recenti)
public sealed class UserRegisteredHandler(
    RecipientStore recipients,
    NotificationSender sender,
    EmailTemplates templates,
    TimeProvider timeProvider,
    IOptions<NotificationOptions> options,
    ILogger<UserRegisteredHandler> logger) : IIntegrationEventHandler<UserRegistered>
{
    public async Task HandleAsync(UserRegistered integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        await recipients.UpsertEmailAsync(integrationEvent.UserId, integrationEvent.Email, integrationEvent.RegisteredOnUtc, cancellationToken);

        var welcome = options.Value.Welcome;
        if (!welcome.Enabled)
            return;

        if (timeProvider.GetUtcNow() - integrationEvent.RegisteredOnUtc > welcome.MaxEventAge)
        {
            logger.LogInformation("User {UserId}: registration too old, welcome email skipped", integrationEvent.UserId);
            return;
        }

        //Email corrente: potrebbe essere già cambiata (UserEmailChanged arrivato prima) o l'utente cancellato
        var email = await recipients.GetEmailAsync(integrationEvent.UserId, cancellationToken);
        if (email is null)
            return;

        await sender.SendOnceAsync(
            NotificationKeys.Welcome(integrationEvent.UserId),
            integrationEvent.UserId,
            NotificationTypes.Welcome,
            templates.Welcome(email),
            cancellationToken);
    }
}