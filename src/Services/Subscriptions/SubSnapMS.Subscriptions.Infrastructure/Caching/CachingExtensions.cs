using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace SubSnapMS.Subscriptions.Infrastructure.Caching;

internal static class CachingExtensions
{
    private const string RedisConnectionStringName = "Redis";

    //HybridCache: L1 in memoria(sul dispositivo) + L2 (cache distribuita, dati salvati su container docker redis) su Redis (se configurato). Le durate per voce le decide UserSubscriptionsCache (sezione "Subscriptions:Caching"), qui solo i default e i limiti
    //ovviamnete L1 è velocissima (xk vive sul dispositivo in locale), ma quando cambia (unsa sua voce cambia) a differenza di L2, LE SUE REPLICHE NON VENGONO AVVISATE E NON SI AGGIORNANO

    public static IServiceCollection AddSubscriptionsCaching(this IServiceCollection services, IConfiguration configuration)  //method extension
    {
        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName);  //prendi value da appsettings.json

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(redis =>  //registra nel DI un IDistributedCache implementato da RedisCache: è l’interfaccia standard di .NET per una cache esterna
            {
                var options = ConfigurationOptions.Parse(redisConnectionString);
                options.AbortOnConnectFail = false;   //Redis giù all'avvio non blocca il servizio, si ricollega da solo
                options.ConnectTimeout = 2000;        //apertura connessione. ms: una cache lenta non deve rallentare le richieste
                options.SyncTimeout = 1000;  //un'operazione sync
                options.AsyncTimeout = 1000;  //un'operazione async
                //con 1sec se redis è in difficolta prova 1sec e poi operazione fallisce
                options.ClientName = "subscriptions";  //nome con cui la connessione si presenta a redis
                redis.ConfigurationOptions = options;
                //niente InstanceName: le chiavi hanno già il prefisso "subscriptions:"
            });
        }
        else  //se invece non trova la connessione a redis
        {
            //Senza Redis HybridCache usa solo la memoria: va bene per test e sviluppo, non con più repliche
            services.AddHostedService<MissingRedisWarning>();  //non si registra nessuna IDistributedCache. allora redis lavora solo con L1(cioè cache del processo, che viene sulvata sul dispositivo dell'utente)
        }

        // HybridCache usa in automatico l'IDistributedCache registrato sopra (Redis) come L2
        services.AddHybridCache(hybrid =>  //registra nel service HybridCache, che quando viene creato cerca nel DI un IDistributedCache (se c'è allora lo usa come L2, altrimenti usa SOLO L1)
        {
            hybrid.MaximumPayloadBytes = 1024 * 1024;   //dimensione massima di un valore serializzato (se viene superato allora warning nel log)1 MB: solitamente una mia lista di abbonamenti è molto più piccola
            hybrid.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),  //dopo 10min la voce scade anche in redis, e si rilegge dal db
                LocalCacheExpiration = TimeSpan.FromMinutes(1)  //dopo 1min la copia in memory scade, e la replica la rilegge da redis
            };
        });

        return services;
    }

    //un warning all'avvio rende visibile la configurazione mancante (invece di scoprirlo in produzione)
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