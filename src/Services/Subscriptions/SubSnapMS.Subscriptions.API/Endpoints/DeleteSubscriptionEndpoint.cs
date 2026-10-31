using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Subscriptions.Application.Features.Subscriptions.Delete;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.API.Endpoints;

internal sealed class DeleteSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapDelete("subscriptions/{id:guid}", HandleAsync)
            .WithTags(EndpointTags.Subscriptions)
            .WithName("DeleteSubscription");

    private static async Task<NoContent> HandleAsync(
        Guid id, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeleteSubscriptionCommand(UserId.From(currentUser.UserId), SubscriptionId.From(id)),
            cancellationToken);

        return ApiResponses.NoContent();
    }
}