using SubSnapMS.BuildingBlocks.Messaging;
using SubSnapMS.BuildingBlocks.Observability;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.BuildingBlocks.Persistence.Migrations;
using SubSnapMS.BuildingBlocks.Web;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.UserDeletion;
using SubSnapMS.Identity.API.Authentication;
using SubSnapMS.Identity.API.Endpoints.WellKnown;
using SubSnapMS.Identity.API.Grpc;
using SubSnapMS.Identity.Application;
using SubSnapMS.Identity.Application.IntegrationEventHandlers;
using SubSnapMS.Identity.Infrastructure;
using SubSnapMS.Identity.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ── Trasversali ───────────────────────────────────────────────────────────────
builder.AddObservability(ServiceNames.Identity, observability =>
{
    observability.AdditionalSources.Add("Npgsql");   // query SQL nelle trace
    observability.AdditionalMeters.Add("Npgsql");
});
builder.AddWebDefaults();                             // envelope, errori, JWT, ICurrentUser

// ── Layer del servizio ────────────────────────────────────────────────────────
builder.Services.AddIdentityApplication(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddIdentityJwtValidation();          // Identity valida con le proprie chiavi (niente HTTP verso se stesso)

// ── Messaging: Outbox (pubblica), Inbox + consumer (saga di cancellazione) ────
builder.Services.AddMessaging(builder.Configuration, ServiceNames.Identity, messaging =>
{
    messaging.AddOutbox<IdentityDbContext>();
    messaging.AddInbox<IdentityDbContext>();
    messaging.AddConsumer<UserDataDeleted, UserDataDeletedHandler>();
});

// ── Readiness: senza database il servizio non riceve traffico ─────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("postgres", tags: [HealthCheckTags.Ready]);

// ── Endpoint ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddGrpc();
builder.Services.AddOpenApi();

var app = builder.Build();

// Avviato con --migrate: applica le migrations e termina (job dedicato in produzione)
if (await app.ApplyMigrationsAsync<IdentityDbContext>(args))
    return;

if (await SubSnapMS.Identity.API.Backfill.RepublishEventsCommand.RunIfRequestedAsync(app, args))
    return;

app.UseWebDefaults();      // forwarded headers, eccezioni, status code pages
app.UseObservability();    // X-Correlation-Id, /health/live, /health/ready
app.UseWebSecurity();      // JWT + authorization (FallbackPolicy: tutto protetto)

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();   // /openapi/v1.json

app.MapEndpoints(app.MapGroup("/api/v1"));
app.MapWellKnownEndpoints();
app.MapGrpcService<UserLookupGrpcService>().AllowAnonymous();   // interno: porta 8081, mai esposta

app.Run();

/// <summary>Visibile ai test di integrazione (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;