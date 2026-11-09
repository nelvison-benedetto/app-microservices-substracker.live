using SubSnapMS.BuildingBlocks.Messaging;
using SubSnapMS.BuildingBlocks.Observability;
using SubSnapMS.BuildingBlocks.Web;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Search.API;
using SubSnapMS.Search.API.IntegrationEventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Trasversali
builder.AddObservability(ServiceNames.Search);
builder.AddWebDefaults();

// Elasticsearch: client, indici, indicizzazione, ricerca
builder.Services.AddSearchServices(builder.Configuration);

// Messaging: solo consumer, idempotenti (versioning esterno di Elasticsearch)
builder.Services.AddMessaging(builder.Configuration, ServiceNames.Search, messaging =>
{
    messaging.AddConsumer<SubscriptionCreated, SubscriptionCreatedHandler>();
    messaging.AddConsumer<SubscriptionUpdated, SubscriptionUpdatedHandler>();
    messaging.AddConsumer<SubscriptionDeleted, SubscriptionDeletedHandler>();
    messaging.AddConsumer<UserDeletionRequested, UserDeletionRequestedHandler>();
});

builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseWebDefaults();
app.UseObservability();
app.UseWebSecurity();

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();

app.MapEndpoints(app.MapGroup("/api/v1"));

app.Run();

//Visibile ai test di integrazione (WebApplicationFactory&lt;Program&gt;)
public partial class Program;