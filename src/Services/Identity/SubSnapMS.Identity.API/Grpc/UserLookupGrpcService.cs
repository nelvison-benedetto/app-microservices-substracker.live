using Grpc.Core;
using MediatR;
using SubSnapMS.Contracts.Grpc.Identity.V1;
using SubSnapMS.Identity.Application.Features.Users.GetStatus;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.API.Grpc;

//Implementazione server del contratto Contracts/Protos/identity/v1/user_lookup.proto.
//Chiamata interna (Media prima di emettere un presigned URL). Porta 8081, HTTP/2.
internal sealed class UserLookupGrpcService(ISender sender) : UserLookup.UserLookupBase
{
    public override async Task<GetUserStatusResponse> GetUserStatus(GetUserStatusRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId) || userId == Guid.Empty)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "user_id must be a non-empty UUID."));

        var status = await sender.Send(new GetUserStatusQuery(UserId.From(userId)), context.CancellationToken);

        return new GetUserStatusResponse
        {
            Exists = status.Exists,
            IsActive = status.IsActive,
            PendingDeletion = status.PendingDeletion
        };
    }
}