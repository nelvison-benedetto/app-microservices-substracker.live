using SubSnapMS.BuildingBlocks.Messaging;
using SubSnapMS.BuildingBlocks.Observability;
using SubSnapMS.BuildingBlocks.Web;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Graph.API;
using SubSnapMS.Graph.API.IntegrationEventHandlers;
using SubSnapMS.Graph.API.Neo4j;

var builder = WebApplication.CreateBuilder(args);

// Trasversali
builder.AddObservability(ServiceNames.Graph, observability =>
    observability.AdditionalSources.Add(GraphTelemetry.Name));   // il driver Neo4j non ha strumentazione propria
builder.AddWebDefaults();

// Neo4j: driver, schema, proiezione, raccomandazioni
builder.Services.AddGraphServices(builder.Configuration);

// Messaging: solo consumer, idempotenti (version guard + tombstone)
builder.Services.AddMessaging(builder.Configuration, ServiceNames.Graph, messaging =>
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