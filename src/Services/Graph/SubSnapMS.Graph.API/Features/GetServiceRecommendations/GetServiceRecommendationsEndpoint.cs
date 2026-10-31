using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Graph.API.Recommendations;

namespace SubSnapMS.Graph.API.Features.GetServiceRecommendations;

internal sealed class GetServiceRecommendationsEndpoint : IEndpoint
{
    private const int DefaultLimit = 10;
    private const int MaxLimit = 25;

    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("recommendations/services", HandleAsync)
            .WithTags(EndpointTags.Recommendations)
            .WithName("GetServiceRecommendations");

    private static async Task<Ok<ApiResult<ServiceRecommendationsResponse>>> HandleAsync(
        int? limit,
        ICurrentUser currentUser,
        RecommendationService recommendations,
        CancellationToken cancellationToken)
    {
        var size = limit ?? DefaultLimit;
        if (size is < 1 or > MaxLimit)
            throw new DomainException("recommendations.limit_invalid", $"limit must be between 1 and {MaxLimit}.");

        var result = await recommendations.GetServiceRecommendationsAsync(currentUser.UserId, size, cancellationToken);
        return ApiResponses.Ok(result);
    }
}