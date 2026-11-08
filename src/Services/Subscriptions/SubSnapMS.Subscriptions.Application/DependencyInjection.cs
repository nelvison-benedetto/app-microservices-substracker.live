using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Application;
using SubSnapMS.Subscriptions.Application.Common.Caching;
using SubSnapMS.Subscriptions.Application.Options;

namespace SubSnapMS.Subscriptions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddSubscriptionsApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SubscriptionCachingOptions>()
            .Bind(configuration.GetSection(SubscriptionCachingOptions.SectionName))
            .Validate(o => o.UserSubscriptionsExpiration > TimeSpan.Zero, "Subscriptions:Caching:UserSubscriptionsExpiration must be positive.")
            .Validate(o => o.LocalCacheExpiration >= TimeSpan.Zero, "Subscriptions:Caching:LocalCacheExpiration cannot be negative.")
            .ValidateOnStart();

        //Registrato PRIMA dei behaviors comuni → è il più esterno della pipeline: quando next() ritorna il TransactionBehavior ha già fatto commit, quindi l'invalidazione avviene dopo il salvataggio.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UserSubscriptionsCacheInvalidationBehavior<,>));

        //MediatR (handler + Tracing → Validation → Transaction), validator, domain event handler, TimeProvider
        services.AddApplicationBuildingBlocks(typeof(DependencyInjection).Assembly, configuration["MediatR:LicenseKey"]);

        services.AddScoped<UserSubscriptionsCache>();

        return services;
    }
}