using SubSnapMS.BuildingBlocks.Messaging;
using SubSnapMS.BuildingBlocks.Observability;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.BuildingBlocks.Persistence.Migrations;
using SubSnapMS.BuildingBlocks.Web;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Subscriptions.Application;
using SubSnapMS.Subscriptions.Application.IntegrationEventHandlers;
using SubSnapMS.Subscriptions.Infrastructure;
using SubSnapMS.Subscriptions.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Trasversali ───────────────────────────────────────────────────────────────
builder.AddObservability(ServiceNames.Subscriptions, observability =>
{
    observability.AdditionalSources.Add("Npgsql");   // query SQL nelle trace
    observability.AdditionalMeters.Add("Npgsql");
});
builder.AddWebDefaults();                             // envelope, errori, JWT (chiavi da Identity), ICurrentUser

// ── Layer del servizio ────────────────────────────────────────────────────────
builder.Services.AddSubscriptionsApplication(builder.Configuration);
builder.Services.AddSubscriptionsInfrastructure(builder.Configuration);

// ── Messaging: Outbox (subscription.*.v1), Inbox + consumer della saga di cancellazione ──
builder.Services.AddMessaging(builder.Configuration, ServiceNames.Subscriptions, messaging =>
{
    messaging.AddOutbox<SubscriptionsDbContext>();
    messaging.AddInbox<SubscriptionsDbContext>();
    messaging.AddConsumer<UserDeletionRequested, UserDeletionRequestedHandler>();
});

// ── Readiness: senza database il servizio non riceve traffico ─────────────────
// (Redis no: è solo una cache, se manca si legge dal database)
builder.Services.AddHealthChecks()
    .AddDbContextCheck<SubscriptionsDbContext>("postgres", tags: [HealthCheckTags.Ready]);

// ── Endpoint ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddOpenApi();

var app = builder.Build();

// Avviato con --migrate: applica le migrations e termina (job dedicato in produzione)
if (await app.ApplyMigrationsAsync<SubscriptionsDbContext>(args))
    return;

if (await SubSnapMS.Subscriptions.API.Backfill.RepublishEventsCommand.RunIfRequestedAsync(app, args))
    return;

app.UseWebDefaults();      // forwarded headers, eccezioni, status code pages
app.UseObservability();    // X-Correlation-Id, /health/live, /health/ready
app.UseWebSecurity();      // JWT + authorization (FallbackPolicy: tutto protetto)

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();   // /openapi/v1.json

app.MapEndpoints(app.MapGroup("/api/v1"));

app.Run();

/// <summary>Visibile ai test di integrazione (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;