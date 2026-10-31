using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Search.API.Querying;

namespace SubSnapMS.Search.API.Features.Suggest;

internal sealed class SuggestEndpoint : IEndpoint
{
    private const int DefaultLimit = 8;
    private const int MaxLimit = 10;
    private const int MaxPrefixLength = 50;

    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("search/suggest", HandleAsync)
            .WithTags(EndpointTags.Search)
            .WithName("SuggestSubscriptions");

    private static async Task<Ok<ApiResult<SuggestResponse>>> HandleAsync(
        string? q,
        int? limit,
        ICurrentUser currentUser,
        SubscriptionSearchService searchService,
        CancellationToken cancellationToken)
    {
        var prefix = q?.Trim();
        if (string.IsNullOrEmpty(prefix))
            return ApiResponses.Ok(new SuggestResponse([]));

        if (prefix.Length > MaxPrefixLength)
            throw new DomainException("search.query_too_long", $"Search text cannot exceed {MaxPrefixLength} characters.");

        var size = limit ?? DefaultLimit;
        if (size is < 1 or > MaxLimit)
            throw new DomainException("search.limit_invalid", $"limit must be between 1 and {MaxLimit}.");

        var result = await searchService.SuggestAsync(currentUser.UserId, prefix, size, cancellationToken);
        return ApiResponses.Ok(result);
    }
}