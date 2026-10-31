using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Subscriptions.API.Endpoints.Common;
using SubSnapMS.Subscriptions.Application.Features.Subscriptions.GetUserSubscriptions;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.API.Endpoints;

/// <summary>Tutti gli abbonamenti dell'utente autenticato, dal più recente. Servito dalla cache se presente.</summary>
internal sealed class GetUserSubscriptionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("subscriptions", HandleAsync)
            .WithTags(EndpointTags.Subscriptions)
            .WithName("GetMySubscriptions");

    private static async Task<Ok<ApiResult<IReadOnlyList<SubscriptionResponse>>>> HandleAsync(
        ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var subscriptions = await sender.Send(new GetUserSubscriptionsQuery(UserId.From(currentUser.UserId)), cancellationToken);

        IReadOnlyList<SubscriptionResponse> response = subscriptions.Select(SubscriptionResponse.From).ToList();
        return ApiResponses.Ok(response);
    }
}