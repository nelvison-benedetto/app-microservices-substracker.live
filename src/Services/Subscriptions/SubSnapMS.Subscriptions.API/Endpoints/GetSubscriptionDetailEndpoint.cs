using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Subscriptions.API.Endpoints.Common;
using SubSnapMS.Subscriptions.Application.Features.Subscriptions.GetDetail;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.API.Endpoints;

//Dettaglio con storico. Un abbonamento di un altro utente risponde 404, come uno inesistente
internal sealed class GetSubscriptionDetailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("subscriptions/{id:guid}", HandleAsync)
            .WithTags(EndpointTags.Subscriptions)
            .WithName("GetSubscriptionDetail");

    private static async Task<Ok<ApiResult<SubscriptionDetailResponse>>> HandleAsync(
        Guid id, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var detail = await sender.Send(
            new GetSubscriptionDetailQuery(UserId.From(currentUser.UserId), SubscriptionId.From(id)),
            cancellationToken);

        return ApiResponses.Ok(SubscriptionDetailResponse.From(detail));
    }
}