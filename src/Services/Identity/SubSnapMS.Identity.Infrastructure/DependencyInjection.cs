using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Persistence;
using SubSnapMS.Identity.Application.Abstractions.Queries;
using SubSnapMS.Identity.Application.Abstractions.Security;
using SubSnapMS.Identity.Infrastructure.Background;
using SubSnapMS.Identity.Infrastructure.Persistence;
using SubSnapMS.Identity.Infrastructure.Persistence.Queries;
using SubSnapMS.Identity.Infrastructure.Persistence.Repositories;
using SubSnapMS.Identity.Infrastructure.Security.Keys;
using SubSnapMS.Identity.Infrastructure.Security.Passwords;
using SubSnapMS.Identity.Infrastructure.Security.Tokens;

namespace SubSnapMS.Identity.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "IdentityDb";

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {

        //Persistence

        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is missing.");

        services.AddServiceDbContext<IdentityDbContext>((_, options) =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                // Errori transitori (riavvio di Postgres, rete): ritenta. Outbox e Inbox usano già la execution strategy.
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
            }));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserDeletionRepository, UserDeletionRepository>();
        services.AddScoped<IUserQueries, UserQueries>();


        //Security

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Configure(options =>
            {
                //Issuer e Audience: UNA sola fonte ("Auth"), usata sia per emettere sia per validare
                options.Issuer = configuration["Auth:Issuer"] ?? string.Empty;
                options.Audience = configuration["Auth:Audience"] ?? string.Empty;
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Auth:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Auth:Audience is required.")
            .Validate(o => o.AccessTokenLifetime > TimeSpan.Zero && o.AccessTokenLifetime <= TimeSpan.FromHours(1),
                "Identity:Jwt:AccessTokenLifetime must be between 0 and 1 hour.")
            .ValidateOnStart();

        services.AddSingleton<SigningKeyStore>();
        services.AddSingleton<ISigningKeyStore>(sp => sp.GetRequiredService<SigningKeyStore>());

        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IRefreshTokenFactory, Sha256RefreshTokenFactory>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();


        //Background

        services.AddOptions<UserDeletionMonitorOptions>()
            .Bind(configuration.GetSection(UserDeletionMonitorOptions.SectionName));
        services.AddHostedService<UserDeletionMonitor>();

        return services;
    }
}