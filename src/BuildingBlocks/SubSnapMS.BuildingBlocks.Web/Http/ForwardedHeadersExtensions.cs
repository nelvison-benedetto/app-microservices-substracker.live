using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace SubSnapMS.BuildingBlocks.Web.Http;

//con questo il service (l'ms) puo vedere l'IP reale dell'utente e il protocollo originale (https) (cosi ha tuuute le info che ha bisogno) invece dell'ip del gateway e http.

internal static class ForwardedHeadersExtensions
{
    //I servizi ricevono richieste solo dal gateway (rete Docker "backend", nessuna porta pubblicata).
    //Il gateway scrive X-Forwarded-For (IP reale dell'utente) e X-Forwarded-Proto (https originale).

    public static IServiceCollection AddServiceForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>  //setti le impostazioni che userà il middleware
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;  //di default il middleware non legge niente, quindi gli dici quali header leggere: XForwardedFor → aggiorna RemoteIpAddress, XForwardedProto → aggiorna Request.Scheme
            options.KnownProxies.Clear();  //KnownProxies elenco di ip fidati, qua svuoti lista cosi si fida di tutti, intanto è interno a rete docker e solo ApiGateway comunica i services (gli ms)
            options.KnownIPNetworks.Clear();  //KnownIPNetworks elenco di reti fidate, qua svuoti lista cosi si fida di tutti, intanto è interno a rete docker e solo ApiGateway comunica i services (gli ms)
            options.ForwardLimit = 1;  //e.g. X-Forwarded-For: 6.6.6.6, 93.44.12.7  dove 6.6.6.6 non sai da chi puo essere stato scritto, mentre il secondo è quello scritto dal tuo ApiGateway. quindi con =1 fai un salto e leggi solo il secondo.
        });

        return services;
    }
}