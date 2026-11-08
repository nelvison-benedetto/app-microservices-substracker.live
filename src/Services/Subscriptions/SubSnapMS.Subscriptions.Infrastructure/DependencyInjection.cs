using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Persistence;
using SubSnapMS.Subscriptions.Application.Abstractions.Persistence;
using SubSnapMS.Subscriptions.Application.Abstractions.Queries;
using SubSnapMS.Subscriptions.Infrastructure.Caching;
using SubSnapMS.Subscriptions.Infrastructure.Persistence;
using SubSnapMS.Subscriptions.Infrastructure.Persistence.Queries;
using SubSnapMS.Subscriptions.Infrastructure.Persistence.Repositories;

namespace SubSnapMS.Subscriptions.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "SubscriptionsDb";

    public static IServiceCollection AddSubscriptionsInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        //Persistence
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        services.AddServiceDbContext<SubscriptionsDbContext>((_, options) =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                //errori transitori (riavvio di Postgres, rete): ritenta. Outbox e Inbox usano già la execution strategy
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
            }));

        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISubscriptionQueries, SubscriptionQueries>();

        //Caching: HybridCache (memoria + Redis)
        services.AddSubscriptionsCaching(configuration);  

        return services;
    }
}