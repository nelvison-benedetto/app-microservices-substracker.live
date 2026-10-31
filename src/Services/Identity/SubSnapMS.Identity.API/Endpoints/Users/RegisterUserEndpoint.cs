using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.Identity.Application.Features.Users.Register;

namespace SubSnapMS.Identity.API.Endpoints.Users;

public sealed record RegisterUserRequest(string Email, string Password);

public sealed record RegisterUserResponse(Guid Id, string Email);

internal sealed class RegisterUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("users/register", HandleAsync)
            .AllowAnonymous()
            .WithTags(EndpointTags.Users)
            .WithName("RegisterUser");

    private static async Task<Created<ApiResult<RegisterUserResponse>>> HandleAsync(
        RegisterUserRequest request, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RegisterUserCommand(request.Email, request.Password), cancellationToken);
        return ApiResponses.Created("/api/v1/users/me", new RegisterUserResponse(result.UserId, result.Email));
    }
}