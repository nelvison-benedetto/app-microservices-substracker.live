using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SubSnapMS.Notification.API.Persistence;

internal sealed class MongoHealthCheck(IMongoDatabase database) : IHealthCheck
{
    private static readonly BsonDocumentCommand<BsonDocument> Ping = new(new BsonDocument("ping", 1));

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await database.RunCommandAsync(Ping, cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "MongoDB is unreachable.", exception);
        }
    }
}