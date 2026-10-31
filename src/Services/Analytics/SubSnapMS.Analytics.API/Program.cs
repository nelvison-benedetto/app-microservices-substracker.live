using SubSnapMS.Analytics.API;
using SubSnapMS.Analytics.API.IntegrationEventHandlers;
using SubSnapMS.BuildingBlocks.Messaging;
using SubSnapMS.BuildingBlocks.Observability;
using SubSnapMS.BuildingBlocks.Web;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Contracts.Subscriptions;

var builder = WebApplication.CreateBuilder(args);

// ── Trasversali ───────────────────────────────────────────────────────────────
builder.AddObservability(ServiceNames.Analytics);
builder.AddWebDefaults();

// ── MongoDB (read model) e calcoli ────────────────────────────────────────────
builder.Services.AddAnalyticsServices(builder.Configuration);

// ── Messaging: solo consumer, idempotenti (version guard + tombstone) ─────────
builder.Services.AddMessaging(builder.Configuration, ServiceNames.Analytics, messaging =>
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

/// <summary>Visibile ai test di integrazione (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;