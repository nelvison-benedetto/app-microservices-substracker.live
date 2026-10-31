using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Application;
using SubSnapMS.Identity.Application.Common.Sessions;
using SubSnapMS.Identity.Application.Options;

namespace SubSnapMS.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SessionOptions>()
            .Bind(configuration.GetSection(SessionOptions.SectionName))
            .Validate(o => o.RefreshTokenLifetime > TimeSpan.Zero, "Identity:Sessions:RefreshTokenLifetime must be positive.")
            .Validate(o => o.ReuseGracePeriod >= TimeSpan.Zero, "Identity:Sessions:ReuseGracePeriod cannot be negative.")
            .ValidateOnStart();

        services.AddOptions<UserDeletionOptions>()
            .Bind(configuration.GetSection(UserDeletionOptions.SectionName))
            .PostConfigure(o =>
            {
                // Default applicato solo se la configurazione non specifica nulla
                // (il binding di una lista già popolata aggiungerebbe invece di sostituire)
                if (o.ExpectedServices.Count == 0)
                    o.ExpectedServices.AddRange(UserDeletionOptions.DefaultExpectedServices);
            });

        // MediatR (handler + Tracing → Validation → Transaction), validator, domain event handler, TimeProvider
        services.AddApplicationBuildingBlocks(typeof(DependencyInjection).Assembly, configuration["MediatR:LicenseKey"]);

        services.AddScoped<SessionTokenIssuer>();

        return services;
    }
}