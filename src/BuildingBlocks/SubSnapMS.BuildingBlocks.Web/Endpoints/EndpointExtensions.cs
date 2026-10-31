using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SubSnapMS.BuildingBlocks.Web.Endpoints;

public static class EndpointExtensions
{
    /// <summary>Registra tutte le classi IEndpoint dell'assembly (di solito il progetto .API del servizio).</summary>
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var descriptors = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEndpoint).IsAssignableFrom(type))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));

        services.TryAddEnumerable(descriptors);
        return services;
    }

    /// <summary>
    /// Mappa tutti gli endpoint registrati sul builder indicato.
    /// Esempio: app.MapEndpoints(app.MapGroup("/api/v1")) → le route degli endpoint sono relative a /api/v1.
    /// </summary>
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, IEndpointRouteBuilder? routes = null)
    {
        var target = routes ?? app;

        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())
            endpoint.MapEndpoint(target);

        return app;
    }
}