using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using SubSnapMS.Analytics.API.Calculations;
using SubSnapMS.Analytics.API.Configuration;
using SubSnapMS.Analytics.API.Persistence;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.Contracts;

namespace SubSnapMS.Analytics.API;

public static class DependencyInjection
{
    private const string ConnectionStringName = "AnalyticsDb";

    public static IServiceCollection AddAnalyticsServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<AnalyticsOptions>()
            .Bind(configuration.GetSection(AnalyticsOptions.SectionName))
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _), "Analytics:TimeZone is not a valid time zone.")
            .Validate(o => o.DeletedTombstoneRetention > TimeSpan.Zero, "Analytics:DeletedTombstoneRetention must be positive.")
            .ValidateOnStart();

        // MongoDB
        //Password inclusa nella connection string: in produzione da ConnectionStrings__AnalyticsDb
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        var mongoUrl = new MongoUrl(connectionString);
        if (string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
            throw new InvalidOperationException($"Connection string '{ConnectionStringName}' must include the database name (…/analytics_db).");

        MongoSerialization.Register();

        services.AddSingleton<IMongoClient>(_ =>
        {
            var settings = MongoClientSettings.FromUrl(mongoUrl);
            settings.ApplicationName = ServiceNames.Analytics;
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
            return new MongoClient(settings);
        });
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName));
        services.AddSingleton<AnalyticsDb>();
        services.AddSingleton<SubscriptionReadStore>();
        services.AddHostedService<MongoIndexInitializer>();

        services.AddHealthChecks()
            .AddCheck<MongoHealthCheck>("mongo", tags: [HealthCheckTags.Ready]);

        // Calcoli
        services.AddSingleton<AnalyticsCalendar>();

        return services;
    }
}