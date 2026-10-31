using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Identity.Application.Features.Auth.Logout;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.API.Endpoints.Auth;

public sealed record LogoutRequest(string RefreshToken);

internal sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("auth/logout", HandleAsync)
            .WithTags(EndpointTags.Auth)
            .WithName("Logout");

    private static async Task<NoContent> HandleAsync(
        LogoutRequest request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutCommand(UserId.From(currentUser.UserId), request.RefreshToken), cancellationToken);
        return ApiResponses.NoContent();
    }
}