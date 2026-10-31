using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Features.GetMyMedia;

internal sealed class GetMyMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("media", HandleAsync)
            .WithTags(EndpointTags.Media)
            .WithName("GetMyMedia");

    private static async Task<Ok<ApiResult<IReadOnlyList<MediaFileResponse>>>> HandleAsync(
        string? purpose, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var files = await sender.Send(new GetMyMediaQuery(UserId.From(currentUser.UserId), purpose), cancellationToken);
        return ApiResponses.Ok(files);
    }
}