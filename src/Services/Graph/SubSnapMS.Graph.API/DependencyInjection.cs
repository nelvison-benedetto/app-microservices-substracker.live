using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.Graph.API.Configuration;
using SubSnapMS.Graph.API.Neo4j;
using SubSnapMS.Graph.API.Projection;
using SubSnapMS.Graph.API.Recommendations;

namespace SubSnapMS.Graph.API;

public static class DependencyInjection
{
    public static IServiceCollection AddGraphServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        // Opzioni
        services.AddOptions<Neo4jOptions>()
            .Bind(configuration.GetSection(Neo4jOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.Uri, UriKind.Absolute, out _), "Neo4j:Uri must be an absolute URI (bolt://… or neo4j://…).")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Username) && !string.IsNullOrWhiteSpace(o.Password), "Neo4j:Username and Neo4j:Password are required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Database), "Neo4j:Database is required.")
            .ValidateOnStart();

        services.AddOptions<GraphOptions>()
            .Bind(configuration.GetSection(GraphOptions.SectionName))
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _), "Graph:TimeZone is not a valid time zone.")
            .Validate(o => o.MinUsersForRecommendation >= 2, "Graph:MinUsersForRecommendation must be at least 2 (privacy).")
            .Validate(o => o.DeletedTombstoneRetention > TimeSpan.Zero, "Graph:DeletedTombstoneRetention must be positive.")
            .Validate(o => o.CleanupInterval >= TimeSpan.FromMinutes(1), "Graph:CleanupInterval must be at least 1 minute.")
            .ValidateOnStart();

        // Driver: singleton thread-safe con pool di connessioni; chiuso allo shutdown dal container DI
        services.AddSingleton<IDriver>(serviceProvider =>
        {
            var neo4j = serviceProvider.GetRequiredService<IOptions<Neo4jOptions>>().Value;

            return GraphDatabase.Driver(
                neo4j.Uri,
                AuthTokens.Basic(neo4j.Username, neo4j.Password),
                config => config
                    .WithMaxConnectionPoolSize(neo4j.MaxConnectionPoolSize)
                    .WithConnectionTimeout(TimeSpan.FromSeconds(10))
                    .WithMaxTransactionRetryTime(TimeSpan.FromSeconds(15)));
        });

        services.AddSingleton<GraphClient>();
        services.AddSingleton<GraphSchemaState>();
        services.AddHostedService<SchemaInitializer>();
        services.AddHostedService<GraphCleanupService>();

        services.AddHealthChecks()
            .AddCheck<Neo4jHealthCheck>("neo4j", tags: [HealthCheckTags.Ready]);

        // Proiezione e letture
        services.AddSingleton<GraphProjector>();
        services.AddSingleton<GraphCalendar>();
        services.AddSingleton<RecommendationService>();

        return services;
    }
}