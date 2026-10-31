using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic.FileIO;
using SubSnapMS.BuildingBlocks.Observability.HealthChecks;
using SubSnapMS.Search.API.Configuration;
using SubSnapMS.Search.API.Elasticsearch;
using SubSnapMS.Search.API.Indexing;
using SubSnapMS.Search.API.Querying;
using System.Net.Http.Headers;
using System.Text;

namespace SubSnapMS.Search.API;

public static class DependencyInjection
{
    public static IServiceCollection AddSearchServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        // ── Opzioni ───────────────────────────────────────────────────────────────
        services.AddOptions<ElasticsearchOptions>()
            .Bind(configuration.GetSection(ElasticsearchOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.Uri, UriKind.Absolute, out _), "Elasticsearch:Uri must be an absolute URL.")
            .Validate(o => o.NumberOfReplicas >= 0, "Elasticsearch:NumberOfReplicas cannot be negative.")
            .ValidateOnStart();

        services.AddOptions<SearchOptions>()
            .Bind(configuration.GetSection(SearchOptions.SectionName))
            .Validate(o => TimeZoneInfo.TryFindSystemTimeZoneById(o.TimeZone, out _), "Search:TimeZone is not a valid time zone.")
            .Validate(o => o.DeletedTombstoneRetention > TimeSpan.Zero, "Search:DeletedTombstoneRetention must be positive.")
            .Validate(o => o.CleanupInterval >= TimeSpan.FromMinutes(1), "Search:CleanupInterval must be at least 1 minute.")
            .ValidateOnStart();

        // ── Client HTTP verso Elasticsearch: retry, timeout, circuit breaker ──────
        services.AddHttpClient<ElasticHttpClient>((serviceProvider, client) =>
        {
            var elastic = serviceProvider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;
            client.BaseAddress = new Uri(elastic.Uri.TrimEnd('/') + "/");

            if (!string.IsNullOrWhiteSpace(elastic.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", elastic.ApiKey);
            }
            else if (!string.IsNullOrWhiteSpace(elastic.Username))
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{elastic.Username}:{elastic.Password}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            }
        })
            .AddStandardResilienceHandler(resilience =>
            {
                // Tutte le operazioni sono idempotenti (PUT con versione esterna, delete_by_query, search): il retry è sicuro
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });

        // ── Indici ────────────────────────────────────────────────────────────────
        services.AddSingleton<SearchIndexState>();
        services.AddHostedService<IndexInitializer>();
        services.AddHostedService<TombstoneCleanupService>();

        services.AddHealthChecks()
            .AddCheck<ElasticsearchHealthCheck>("elasticsearch", tags: [HealthCheckTags.Ready]);

        // ── Indicizzazione e ricerca ──────────────────────────────────────────────
        services.AddScoped<SubscriptionIndexer>();
        services.AddScoped<SubscriptionSearchService>();
        services.AddSingleton<SearchCalendar>();

        return services;
    }
}