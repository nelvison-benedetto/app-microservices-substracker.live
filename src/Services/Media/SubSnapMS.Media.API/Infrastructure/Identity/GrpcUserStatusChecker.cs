using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.Contracts.Grpc.Identity.V1;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Infrastructure.Identity;

//Chiama Identity.UserLookup via gRPC (porta 8081, HTTP/2, rete interna).
//Retry, timeout e circuit breaker sono nel client (AddStandardResilienceHandler).
//Se Identity non è raggiungibile l'eccezione risale (500): per un'operazione di scrittura è più sicuro rifiutare che lasciar caricare file senza verifica (fail closed).
internal sealed class GrpcUserStatusChecker(UserLookup.UserLookupClient client) : IUserStatusChecker
{
    public async Task EnsureCanUploadAsync(UserId userId, CancellationToken cancellationToken)
    {
        var status = await client.GetUserStatusAsync(
            new GetUserStatusRequest { UserId = userId.Value.ToString() },
            cancellationToken: cancellationToken);

        if (!status.Exists || !status.IsActive || status.PendingDeletion)
            throw new ForbiddenException("user.not_active", "This account cannot upload files.");
    }
}