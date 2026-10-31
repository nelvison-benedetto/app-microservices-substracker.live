using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.Core.Common;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.DeleteMedia;

internal sealed class DeleteMediaEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapDelete("media/{id:guid}", HandleAsync)
            .WithTags(EndpointTags.Media)
            .WithName("DeleteMedia");

    private static async Task<NoContent> HandleAsync(
        Guid id, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteMediaCommand(UserId.From(currentUser.UserId), MediaFileId.From(id)), cancellationToken);
        return ApiResponses.NoContent();
    }
}