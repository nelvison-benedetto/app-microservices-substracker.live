using SubSnapMS.BuildingBlocks.Application;
using SubSnapMS.BuildingBlocks.Messaging;
using SubSnapMS.BuildingBlocks.Observability;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.BuildingBlocks.Persistence.Migrations;
using SubSnapMS.BuildingBlocks.Web;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.Contracts;
using SubSnapMS.Contracts.Identity;
using SubSnapMS.Contracts.Media;
using SubSnapMS.Media.API.Infrastructure;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.API.IntegrationEventHandlers;

var builder = WebApplication.CreateBuilder(args);

// Trasversali
builder.AddObservability(ServiceNames.Media, observability =>
{
    observability.AdditionalSources.Add("Npgsql");
    observability.AdditionalMeters.Add("Npgsql");
});
builder.AddWebDefaults();

// Application (in questo progetto): MediatR, validator, domain event handler
builder.Services.AddApplicationBuildingBlocks(typeof(Program).Assembly, builder.Configuration["MediatR:LicenseKey"]);

// Infrastructure: database, storage R2, client gRPC verso Identity, pulizia
builder.Services.AddMediaInfrastructure(builder.Configuration);

// Messaging
builder.Services.AddMessaging(builder.Configuration, ServiceNames.Media, messaging =>
{
    messaging.AddOutbox<MediaDbContext>();
    messaging.AddInbox<MediaDbContext>();
    messaging.AddConsumer<UserDeletionRequested, UserDeletionRequestedHandler>();   // saga di cancellazione account
    messaging.AddConsumer<MediaDeleted, MediaDeletedHandler>();                     // cancella il file da R2 DOPO il commit
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MediaDbContext>("postgres", tags: [HealthCheckTags.Ready]);

builder.Services.AddEndpoints(typeof(Program).Assembly);
builder.Services.AddOpenApi();

var app = builder.Build();

if (await app.ApplyMigrationsAsync<MediaDbContext>(args))
    return;

app.UseWebDefaults();
app.UseObservability();
app.UseWebSecurity();

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous();

app.MapEndpoints(app.MapGroup("/api/v1"));

app.Run();

//Visibile ai test di integrazione (WebApplicationFactory&lt;Program&gt;)
public partial class Program;