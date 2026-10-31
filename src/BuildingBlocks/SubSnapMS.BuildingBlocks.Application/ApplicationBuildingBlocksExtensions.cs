using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SubSnapMS.BuildingBlocks.Application.Behaviors;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;

namespace SubSnapMS.BuildingBlocks.Application;

public static class ApplicationBuildingBlocksExtensions
{
    /// <summary>
    /// Registra MediatR (handler dell'assembly + pipeline Tracing → Validation → Transaction),
    /// i validator FluentValidation, gli handler dei domain event e TimeProvider.
    /// IUnitOfWork NON è registrato qui: lo registra l'Infrastructure del servizio (il suo DbContext).
    /// </summary>
    /// <param name="applicationAssembly">L'assembly .Application del servizio (handler, validator, domain event handler).</param>
    /// <param name="mediatRLicenseKey">Chiave della Community License di MediatR (senza: solo un warning nei log).</param>
    public static IServiceCollection AddApplicationBuildingBlocks(
        this IServiceCollection services,
        Assembly applicationAssembly,
        string? mediatRLicenseKey)
    {
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        // Orologio di sistema; nei test si sostituisce con FakeTimeProvider
        services.TryAddSingleton(TimeProvider.System);

        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(applicationAssembly);

            if (!string.IsNullOrWhiteSpace(mediatRLicenseKey))
                configuration.LicenseKey = mediatRLicenseKey;

            // Ordine di registrazione = dall'esterno verso l'interno
            configuration.AddOpenBehavior(typeof(TracingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        // Validator anche internal: restano dentro il servizio
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        RegisterDomainEventHandlers(services, applicationAssembly);

        return services;
    }

    private static void RegisterDomainEventHandlers(IServiceCollection services, Assembly assembly)
    {
        var handlerInterface = typeof(IDomainEventHandler<>);

        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerInterface)
                .Select(i => (Service: i, Implementation: type)));

        // Scoped: usano repository e DbContext della richiesta corrente
        foreach (var (service, implementation) in registrations)
            services.TryAddEnumerable(ServiceDescriptor.Scoped(service, implementation));
    }
}