using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace SubSnapMS.BuildingBlocks.Web.Http;

internal static class ForwardedHeadersExtensions
{
    /// <summary>
    /// I servizi ricevono richieste solo dal gateway (rete Docker "backend", nessuna porta pubblicata).
    /// Il gateway scrive X-Forwarded-For (IP reale dell'utente) e X-Forwarded-Proto (https originale).
    /// </summary>
    public static IServiceCollection AddServiceForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            options.ForwardLimit = 1;
        });

        return services;
    }
}