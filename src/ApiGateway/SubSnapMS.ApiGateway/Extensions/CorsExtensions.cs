using Microsoft.Extensions.Options;
using SubSnapMS.ApiGateway.Configuration;

namespace SubSnapMS.ApiGateway.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(GatewayCorsOptions.SectionName);
        services.Configure<GatewayCorsOptions>(section);

        var options = section.Get<GatewayCorsOptions>() ?? new GatewayCorsOptions();
        if (!options.Enabled)
            return services;

        services.AddCors(cors => cors.AddDefaultPolicy(policy => policy
            .WithOrigins(options.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("X-Correlation-Id", "Retry-After")
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));
        // Niente AllowCredentials: l'autenticazione è un header Bearer, non un cookie

        return services;
    }

    public static WebApplication UseGatewayCors(this WebApplication app)
    {
        if (app.Services.GetRequiredService<IOptions<GatewayCorsOptions>>().Value.Enabled)
            app.UseCors();   // prima dell'autenticazione: le preflight OPTIONS non hanno token

        return app;
    }
}