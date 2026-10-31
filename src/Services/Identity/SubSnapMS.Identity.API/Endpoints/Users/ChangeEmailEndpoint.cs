using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Identity.Application.Features.Users.ChangeEmail;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.API.Endpoints.Users;

public sealed record ChangeEmailRequest(string NewEmail, string CurrentPassword);

internal sealed class ChangeEmailEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPut("users/me/email", HandleAsync)
            .WithTags(EndpointTags.Users)
            .WithName("ChangeMyEmail");

    private static async Task<NoContent> HandleAsync(
        ChangeEmailRequest request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(
            new ChangeEmailCommand(UserId.From(currentUser.UserId), request.NewEmail, request.CurrentPassword),
            cancellationToken);

        return ApiResponses.NoContent();
    }
}