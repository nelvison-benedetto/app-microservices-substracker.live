using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Identity.Application.Features.Auth.LogoutEverywhere;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.API.Endpoints.Auth;

internal sealed class LogoutEverywhereEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("auth/logout-all", HandleAsync)
            .WithTags(EndpointTags.Auth)
            .WithName("LogoutEverywhere");

    private static async Task<NoContent> HandleAsync(
        ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new LogoutEverywhereCommand(UserId.From(currentUser.UserId)), cancellationToken);
        return ApiResponses.NoContent();
    }
}