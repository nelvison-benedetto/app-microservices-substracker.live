using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SubSnapMS.Subscriptions.Infrastructure.Caching;

internal static class CachingExtensions
{
    private const string RedisConnectionStringName = "Redis";

    /// <summary>
    /// HybridCache: L1 in memoria + L2 su Redis (se configurato). Le durate per voce le decide
    /// UserSubscriptionsCache (sezione "Subscriptions:Caching"); qui solo i default e i limiti.
    /// </summary>
    public static IServiceCollection AddSubscriptionsCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName);

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(redis =>
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;   // Redis giù all'avvio non blocca il servizio: si ricollega da solo
                options.ConnectTimeout = 2000;        // ms: una cache lenta non deve rallentare le richieste
                options.SyncTimeout = 1000;
                options.AsyncTimeout = 1000;
                options.ClientName = "subscriptions";
                redis.ConfigurationOptions = options;
                // Niente InstanceName: le chiavi hanno già il prefisso "subscriptions:"
            });
        }
        else
        {
            // Senza Redis HybridCache usa solo la memoria: va bene per test e sviluppo, non con più repliche
            services.AddHostedService<MissingRedisWarning>();
        }

        // HybridCache usa in automatico l'IDistributedCache registrato sopra (Redis) come L2
        services.AddHybridCache(hybrid =>
        {
            hybrid.MaximumPayloadBytes = 1024 * 1024;   // 1 MB: una lista di abbonamenti è molto più piccola
            hybrid.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(1)
            };
        });

        return services;
    }

    /// <summary>Un warning all'avvio rende visibile la configurazione mancante (invece di scoprirlo in produzione).</summary>
    private sealed class MissingRedisWarning(ILogger<MissingRedisWarning> logger) : Microsoft.Extensions.Hosting.IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogWarning("ConnectionStrings:{Name} is not set: the subscriptions cache is in-memory only (L1).", RedisConnectionStringName);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}