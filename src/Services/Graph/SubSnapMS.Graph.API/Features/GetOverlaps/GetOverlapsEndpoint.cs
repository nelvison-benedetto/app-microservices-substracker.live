using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Graph.API.Recommendations;

namespace SubSnapMS.Graph.API.Features.GetOverlaps;

internal sealed class GetOverlapsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("recommendations/overlaps", HandleAsync)
            .WithTags(EndpointTags.Recommendations)
            .WithName("GetSubscriptionOverlaps");

    private static async Task<Ok<ApiResult<OverlapsResponse>>> HandleAsync(
        ICurrentUser currentUser,
        RecommendationService recommendations,
        CancellationToken cancellationToken)
    {
        var result = await recommendations.GetOverlapsAsync(currentUser.UserId, cancellationToken);
        return ApiResponses.Ok(result);
    }
}