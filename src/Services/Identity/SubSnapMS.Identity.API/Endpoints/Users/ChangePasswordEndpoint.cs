using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Identity.API.Endpoints.Common;
using SubSnapMS.Identity.Application.Features.Users.ChangePassword;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.API.Endpoints.Users;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

//Tutte le sessioni vengono chiuse: la risposta contiene i token della nuova sessione di questo dispositivo
internal sealed class ChangePasswordEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPut("users/me/password", HandleAsync)
            .WithTags(EndpointTags.Users)
            .WithName("ChangeMyPassword");

    private static async Task<Ok<ApiResult<AuthTokensResponse>>> HandleAsync(
        ChangePasswordRequest request, ICurrentUser currentUser, ISender sender, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(
            new ChangePasswordCommand(UserId.From(currentUser.UserId), request.CurrentPassword, request.NewPassword),
            cancellationToken);

        return ApiResponses.Ok(AuthTokensResponse.From(tokens, timeProvider.GetUtcNow()));
    }
}