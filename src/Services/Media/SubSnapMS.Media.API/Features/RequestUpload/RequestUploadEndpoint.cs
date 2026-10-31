using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Media.API.Common;
using SubSnapMS.Media.Core.Common;

namespace SubSnapMS.Media.API.Features.RequestUpload;

/// <param name="Purpose">avatar</param>
/// <param name="ContentType">image/jpeg | image/png | image/webp</param>
/// <param name="SizeBytes">Dimensione dichiarata; quella reale viene verificata alla conferma.</param>
public sealed record RequestUploadRequest(string Purpose, string ContentType, long SizeBytes);

internal sealed class RequestUploadEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("media/upload-url", HandleAsync)
            .WithTags(EndpointTags.Media)
            .WithName("RequestMediaUpload");

    private static async Task<Ok<ApiResult<UploadUrlResponse>>> HandleAsync(
        RequestUploadRequest request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var response = await sender.Send(
            new RequestUploadCommand(UserId.From(currentUser.UserId), request.Purpose, request.ContentType, request.SizeBytes),
            cancellationToken);

        return ApiResponses.Ok(response);
    }
}