using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Persistence;
using SubSnapMS.Contracts.Grpc.Identity.V1;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.API.Infrastructure.Background;
using SubSnapMS.Media.API.Infrastructure.Identity;
using SubSnapMS.Media.API.Infrastructure.Persistence;
using SubSnapMS.Media.API.Infrastructure.Storage;

namespace SubSnapMS.Media.API.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMediaInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("MediaDb")
            ?? throw new InvalidOperationException("Connection string 'MediaDb' is missing.");

        services.AddServiceDbContext<MediaDbContext>((_, options) =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
                npgsql.MigrationsHistoryTable("__ef_migrations_history");
            }));

        // Upload
        services.AddOptions<MediaUploadOptions>()
            .Bind(configuration.GetSection(MediaUploadOptions.SectionName))
            .Validate(o => o.UploadUrlLifetime > TimeSpan.Zero && o.UploadUrlLifetime <= TimeSpan.FromHours(1),
                "Media:Uploads:UploadUrlLifetime must be between 0 and 1 hour.")
            .ValidateOnStart();

        services.AddHostedService<PendingUploadCleanupService>();

        // Storage (Cloudflare R2 / qualsiasi S3 compatibile, es. MinIO in sviluppo) 
        services.AddOptions<ObjectStorageOptions>()
            .Bind(configuration.GetSection(ObjectStorageOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "Storage:BucketName is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.PublicBaseUrl), "Storage:PublicBaseUrl is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.ServiceUrl) || !string.IsNullOrWhiteSpace(o.AccountId),
                "Storage: set ServiceUrl or AccountId (Cloudflare R2).")
            .ValidateOnStart();

        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var storage = serviceProvider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;

            var config = new AmazonS3Config
            {
                ServiceURL = storage.ResolveServiceUrl(),
                ForcePathStyle = storage.ForcePathStyle,
                AuthenticationRegion = "auto",
                // R2 non supporta i checksum aggiunti per default dalle versioni recenti dell'SDK
                RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
                ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED
            };

            return new AmazonS3Client(new BasicAWSCredentials(storage.AccessKeyId, storage.SecretAccessKey), config);
        });

        services.AddSingleton<IObjectStorage, S3ObjectStorage>();

        // gRPC verso Identity (sincrono): timeout brevi, retry, circuit breaker
        var identityGrpcAddress = configuration["Services:Identity:GrpcAddress"]
            ?? throw new InvalidOperationException("Services:Identity:GrpcAddress is missing.");

        services.AddGrpcClient<UserLookup.UserLookupClient>(client => client.Address = new Uri(identityGrpcAddress))
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(6);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            });

        services.AddScoped<IUserStatusChecker, GrpcUserStatusChecker>();

        return services;
    }
}