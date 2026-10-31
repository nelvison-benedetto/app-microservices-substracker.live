using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.Identity.API.Endpoints.Common;
using SubSnapMS.Identity.Application.Features.Auth.Login;

namespace SubSnapMS.Identity.API.Endpoints.Auth;

public sealed record LoginRequest(string Email, string Password);

internal sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("auth/login", HandleAsync)
            .AllowAnonymous()
            .WithTags(EndpointTags.Auth)
            .WithName("Login");

    private static async Task<Ok<ApiResult<AuthTokensResponse>>> HandleAsync(
        LoginRequest request, ISender sender, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var tokens = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        return ApiResponses.Ok(AuthTokensResponse.From(tokens, timeProvider.GetUtcNow()));
    }
}