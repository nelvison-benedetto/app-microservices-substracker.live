using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Search.API.Querying;

namespace SubSnapMS.Search.API.Features.SearchSubscriptions;

internal sealed class SearchSubscriptionsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("search/subscriptions", HandleAsync)
            .WithTags(EndpointTags.Search)
            .WithName("SearchSubscriptions");

    private static async Task<Ok<ApiResult<SearchSubscriptionsResponse>>> HandleAsync(
        string? q,
        string? category,
        string? billingCycle,
        string? currency,
        string? status,
        decimal? minMonthlyAmount,
        decimal? maxMonthlyAmount,
        string? sort,
        int? page,
        int? pageSize,
        ICurrentUser currentUser,
        SubscriptionSearchService searchService,
        CancellationToken cancellationToken)
    {
        var criteria = SubscriptionSearchCriteria.Create(
            q, category, billingCycle, currency, status, minMonthlyAmount, maxMonthlyAmount, sort, page, pageSize);

        var result = await searchService.SearchAsync(currentUser.UserId, criteria, cancellationToken);
        return ApiResponses.Ok(result);
    }
}