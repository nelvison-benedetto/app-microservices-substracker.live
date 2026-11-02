using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SubSnapMS.BuildingBlocks.Observability.HealthChecks;

internal static class HealthCheckExtensions
{
    internal const string LivePath = "/health/live";
    internal const string ReadyPath = "/health/ready";


    internal static IServiceCollection AddDefaultHealthChecks(this IServiceCollection services)  ////method extension, viene aggiunto a 'services'
    {
        // Controllo minimo: se il processo risponde, è vivo
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: [HealthCheckTags.Live]);  //aggiunge un health check chiamato "self" che ritorna sempre Healthy, con tag "live", cmnq se il processo è morto il code non legge nemmeno questa lambda

        return services;
    }

    internal static WebApplication MapDefaultHealthChecks(this WebApplication app)  //method extension, viene aggiunto a 'WebApplication' 
    {
        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Live),  //decide quali check esegue questo endpoint, here solo quelli con tag "live"
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }).AllowAnonymous();  //AllowAnonymous xk e.g. yarp e docker non hanno un jwt valido , ma vogliamo che possano fare la health check senza autenticazione
        //crea un endpoint (come MapGet), che ad ogni req chiede a HealthCheckService di eseguire i checks

        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = HealthCheckResponseWriter.WriteAsync
        }).AllowAnonymous();
        //se Predicate non seleziona nessun check, HealthReport.Entries è vuoto e lo stato complessivo è Healthy, quindi "nessuna dipendenza da verificare, quindi pronto".

        return app;
    }
}

