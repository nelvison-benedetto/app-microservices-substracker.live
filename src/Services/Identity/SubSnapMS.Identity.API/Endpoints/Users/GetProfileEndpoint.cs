using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Identity.Application.Features.Users.GetProfile;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.API.Endpoints.Users;

//Stessi campi del monolite (id, email, isActive, createdAt, updatedAt, lastLogin) + status
public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    bool IsActive,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLogin);

internal sealed class GetProfileEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("users/me", HandleAsync)
            .WithTags(EndpointTags.Users)
            .WithName("GetMyProfile");

    private static async Task<Ok<ApiResult<UserProfileResponse>>> HandleAsync(
        ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var profile = await sender.Send(new GetUserProfileQuery(UserId.From(currentUser.UserId)), cancellationToken);

        return ApiResponses.Ok(new UserProfileResponse(
            profile.Id,
            profile.Email,
            profile.Status == nameof(UserStatus.Active),
            profile.Status,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.LastLoginAtUtc));
    }
}