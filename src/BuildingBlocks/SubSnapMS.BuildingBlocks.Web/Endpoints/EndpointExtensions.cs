using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SubSnapMS.BuildingBlocks.Web.Endpoints;

public static class EndpointExtensions
{
    
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)  //extension method
    {
        var descriptors = assembly.GetTypes()  //reflection, return tutti i types definiti nell'assembly (sia pubblici, internal e annidati)
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IEndpoint).IsAssignableFrom(type))  //filtro type.IsClass && !type.IsAbstract, e verifica 'type può implementare l’interfaccia IEndpoint'
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type));  //per ogni classe trovata crea un ServiceDescriptor (che dice nel DI “per il servizio IEndpoint, usa l’implementazione RequestUploadEndpoint, con lifetime transient”)

        services.TryAddEnumerable(descriptors);  //aggiunge le registrazioni, Enumerable (xk qua ci sono molte implemnetazioni di IEndopoint, e devi farle tutte. Try... prima di aggiungere controlle che non sia gia stata fatta per non creare duplicati)
        return services;
    }
    //registra tutte le classi IEndpoint dell'assembly (di solito il progetto .API del servizio)


    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app, IEndpointRouteBuilder? routes = null)
    {
        var target = routes ?? app;  //route è e.g. /api/v1, ed è assoutamente sempre da passare. in caso fallback allora usa l'app stessa (route senza prefisso)
        foreach (var endpoint in app.ServiceProvider.GetServices<IEndpoint>())  //app.ServiceProvider è il container DI gia costruito in program con Build(), GetServices<IEndpoint>() return un'istanza per ogni implementazione registrata da AddEndpoints
            endpoint.MapEndpoint(target);  //l'endpoint esegue il suo MapPost/MapGet sul gruppo
        return app;
    }
    //mappa tutti gli endpoint registrati sul builder indicato, e.g. app.MapEndpoints(app.MapGroup("/api/v1")) → le route degli endpoint sono relative a /api/v1


}

