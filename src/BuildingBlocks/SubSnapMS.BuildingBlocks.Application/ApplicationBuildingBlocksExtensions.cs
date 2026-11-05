using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SubSnapMS.BuildingBlocks.Application.Behaviors;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;

namespace SubSnapMS.BuildingBlocks.Application;

public static class ApplicationBuildingBlocksExtensions
{
    //accende il livello Application di un service: registra MediatR (handler dell'assembly + pipeline Tracing → Validation → Transaction), i validator FluentValidation, gli handler dei domain event e TimeProvider.
    //IUnitOfWork NON è registrato qui: lo registra l'Infrastructure del servizio (il suo DbContext).

    public static IServiceCollection AddApplicationBuildingBlocks(
        this IServiceCollection services,
        Assembly applicationAssembly,
        string? mediatRLicenseKey)   //extension method
    {
        ArgumentNullException.ThrowIfNull(applicationAssembly);  

        services.TryAddSingleton(TimeProvider.System);  //non leggi l'ora da DateTimeOffset.UtcNow, ma invece usi un Timeprovider iniettato. (per i test potresti anche usare FakeTimeProvider)

        services.AddMediatR(configuration =>   //registra MediatR nel DI
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);  //scan assembly e registra ogni class che implementa le classi di MediatR
            if (!string.IsNullOrWhiteSpace(mediatRLicenseKey))
                configuration.LicenseKey = mediatRLicenseKey;  //sul sito ufficiale di mediatr puoi prendere la key se sei con licenza community (<5M/anno), cmnq anche senza mediatr funziona cmnq.

            //!!ordine di registrazione esterno -> verso l'interno
            configuration.AddOpenBehavior(typeof(TracingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        //Validator anche internal: restano dentro il servizio
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);  //cerca ogni classe che eredita da AbstractValidator<T> e la registra come IValidator<T> 

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        RegisterDomainEventHandlers(services, applicationAssembly);  //gli handlers li registra RegisterDomainEventHandlers() vedi qua sotto

        return services;
    }

    private static void RegisterDomainEventHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(IDomainEventHandler<>); //reflection tecnique, ottieni il tipo generico aperto IDomainEventHandler<>, che userai come riferimento per i confronti.
        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })  //tutti i tipi dell'assembly, filtrati x class (no interfaces/struct/enum), istanziabili, no classi generiche aperte (es. MyHandler<T>)(xk il DI non saprebbe con quale T crearle)
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (Service: i, Implementation: type)));  //x ogni classe: tutte le interfaces che implementa --> tieni solo quelle sono un “un IDomainEventHandler di qualcosa” --> verifica che sia generica, e toglie l'argomento dentro <> (e.g.IDomainEventHandler<SubscriptionCreatedDomainEvent> → IDomainEventHandler<>) --> infine crea una tupla con nomi (e.g.[IDomainEventHandler<SubscriptionCreatedDomainEvent>, SubscriptionCreatedDomainEventHandler)])
        foreach (var (service, implementation) in registrations)
            services.TryAddEnumerable(ServiceDescriptor.Scoped(service, implementation));
        //decostruisci la tupla, poi “per l’interfaccia service, crea implementation, una per scope” (scoped xk gli handler usano repository e outbox, che dipendono dal dbcontext della req).

    }
    //mediatr già registra i suoi handlers da solo, ma gli IDomainEventHandler<T> li ho fatti io, quindi devo registrarli manualmente.
}