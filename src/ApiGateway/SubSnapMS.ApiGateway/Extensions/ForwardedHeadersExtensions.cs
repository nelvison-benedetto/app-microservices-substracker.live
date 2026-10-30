using Microsoft.AspNetCore.HttpOverrides;

namespace SubSnapMS.ApiGateway.Extensions;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Il gateway riceve richieste da due proxy fidati sulla rete Docker "edge":
    ///  - Traefik (app mobile / API pubblica): TLS terminato da Traefik, che scrive X-Forwarded-For e X-Forwarded-Proto;
    ///  - Next.js (Server Actions): inoltra in X-Forwarded-For l'IP del browser ricevuto da Traefik.
    /// In entrambi i casi l'ultimo valore di X-Forwarded-For è l'IP reale dell'utente.
    /// </summary>
    public static IServiceCollection AddGatewayForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Il gateway non ha porte pubblicate: lo raggiungono solo container della rete Docker privata
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            // Solo l'ultimo valore di X-Forwarded-For: quello scritto dal proxy fidato
            options.ForwardLimit = 1;
        });

        return services;
    }
}
