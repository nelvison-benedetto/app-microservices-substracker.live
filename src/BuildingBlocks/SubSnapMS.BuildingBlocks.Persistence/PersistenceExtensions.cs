using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Application.Persistence;

namespace SubSnapMS.BuildingBlocks.Persistence;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registra il DbContext del servizio (scoped) e lo espone anche come IUnitOfWork:
    /// TransactionBehavior, repository, Outbox e Inbox usano la stessa istanza nella stessa richiesta.
    /// </summary>
    /// <param name="configure">Provider e opzioni, es. options.UseNpgsql(connectionString, ...).</param>
    public static IServiceCollection AddServiceDbContext<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure)
        where TContext : ServiceDbContext
    {
        ArgumentNullException.ThrowIfNull(configure);

        // Niente pooling: il costruttore riceve servizi scoped (IDomainEventDispatcher)
        services.AddDbContext<TContext>(configure);
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<TContext>());

        return services;
    }
}