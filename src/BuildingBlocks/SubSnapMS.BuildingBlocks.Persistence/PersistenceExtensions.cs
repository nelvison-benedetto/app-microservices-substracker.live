using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Application.Persistence;

namespace SubSnapMS.BuildingBlocks.Persistence;

public static class PersistenceExtensions
{
    //Registra il DbContext del servizio (scoped) e lo espone anche come IUnitOfWork: TransactionBehavior, repository, Outbox e Inbox usano la stessa istanza nella stessa richiesta.

    public static IServiceCollection AddServiceDbContext<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configure)  //function con cui il servizio sceglie provider e opzioni
        where TContext : ServiceDbContext  //il DbContext del servizio deve derivare da ServiceDbContext. ServiceDbContext implementa IUnitOfWork
    {
        ArgumentNullException.ThrowIfNull(configure); //senza la funct di configurazione il DbContext non saprebbe a quale db collegarsi

        // Niente pooling: il costruttore riceve servizi scoped (IDomainEventDispatcher)
        services.AddDbContext<TContext>(configure);  //lo aggiunge al DI, di default con Scoped lifetime (una istanza per req http)
        services.AddScoped<IUnitOfWork>(serviceProvider => serviceProvider.GetRequiredService<TContext>());  //registra nel DI, cosi quando qualcuno chiede IUnityofWork riceve la stessa istanza del DbContext del servizio (che implementa IUnitOfWork)

        return services;
    }

}