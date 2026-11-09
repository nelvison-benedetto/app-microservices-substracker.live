using SubSnapMS.BuildingBlocks.Messaging;
using SubSnapMS.BuildingBlocks.Observability;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Notification.API;
using SubSnapMS.Notification.API.Diagnostics;
using SubSnapMS.Notification.API.IntegrationEventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Trasversali
builder.AddObservability(ServiceNames.Notification, observability =>
{
    observability.AdditionalSources.Add(NotificationTelemetry.Name);
    observability.AdditionalMeters.Add(NotificationTelemetry.Name);
});

// MongoDB, email, promemoria di rinnovo
builder.Services.AddNotificationServices(builder.Configuration);

//Messaging: solo consumer (nessun Outbox: Notification non pubblica eventi)
//Nessuna Inbox: gli handler sono idempotenti (upsert con guardia + registro invii su Mongo)
builder.Services.AddMessaging(builder.Configuration, ServiceNames.Notification, messaging =>
{
    messaging.AddConsumer<UserRegistered, UserRegisteredHandler>();
    messaging.AddConsumer<UserEmailChanged, UserEmailChangedHandler>();
    messaging.AddConsumer<UserDeletionRequested, UserDeletionRequestedHandler>();
    messaging.AddConsumer<SubscriptionCreated, SubscriptionCreatedHandler>();
    messaging.AddConsumer<SubscriptionUpdated, SubscriptionUpdatedHandler>();
    messaging.AddConsumer<SubscriptionDeleted, SubscriptionDeletedHandler>();
});

var app = builder.Build();

//Solo X-Correlation-Id e /health/live, /health/ready: nessun endpoint pubblico
app.UseObservability();

app.Run();

//Visibile ai test di integrazione (WebApplicationFactory&lt;Program&gt;)
public partial class Program;