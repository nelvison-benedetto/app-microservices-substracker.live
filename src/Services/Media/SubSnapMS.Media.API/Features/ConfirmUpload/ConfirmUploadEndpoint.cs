using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.Core.Common;
using SubSnapMS.Media.Core.MediaFiles;

namespace SubSnapMS.Media.API.Features.ConfirmUpload;

internal sealed class ConfirmUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("media/{id:guid}/confirm", HandleAsync)
            .WithTags(EndpointTags.Media)
            .WithName("ConfirmMediaUpload");

    private static async Task<Ok<ApiResult<MediaFileResponse>>> HandleAsync(
        Guid id, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var file = await sender.Send(
            new ConfirmUploadCommand(UserId.From(currentUser.UserId), MediaFileId.From(id)),
            cancellationToken);

        return ApiResponses.Ok(file);
    }
}