using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.Identity.API.Endpoints.Common;
using SubSnapMS.Identity.Application.Common.Errors;
using SubSnapMS.Identity.Application.Features.Auth.RefreshSession;
using SubSnapMS.Identity.Core.RefreshTokens;

namespace SubSnapMS.Identity.API.Endpoints.Auth;

public sealed record RefreshSessionRequest(string RefreshToken);

internal sealed class RefreshSessionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("auth/refresh", HandleAsync)
            .AllowAnonymous()
            .WithTags(EndpointTags.Auth)
            .WithName("RefreshSession");

    private static async Task<Ok<ApiResult<AuthTokensResponse>>> HandleAsync(
        RefreshSessionRequest request, ISender sender, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshSessionCommand(request.RefreshToken), cancellationToken);

        //A questo punto la transazione è già stata salvata (es. revoca della famiglia per reuse detection): lanciare qui produce il 401 senza annullare quel salvataggio.
        if (!result.Succeeded)
        {
            throw result.Status switch
            {
                RefreshTokenRotationStatus.Expired => new InvalidRefreshTokenException("auth.refresh_token_expired"),
                RefreshTokenRotationStatus.ReuseDetected => new InvalidRefreshTokenException("auth.refresh_token_reused"),
                RefreshTokenRotationStatus.UserNotActive => new InvalidRefreshTokenException("user.not_active"),
                _ => new InvalidRefreshTokenException()
            };
        }

        return ApiResponses.Ok(AuthTokensResponse.From(result.Tokens!, timeProvider.GetUtcNow()));
    }
}