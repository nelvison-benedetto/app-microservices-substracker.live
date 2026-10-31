using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Queries;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Application.Features.Users.GetProfile;

internal sealed class GetUserProfileHandler(IUserQueries queries) : IQueryHandler<GetUserProfileQuery, UserProfileResult>
{
    public async Task<UserProfileResult> Handle(GetUserProfileQuery query, CancellationToken cancellationToken) =>
        await queries.GetProfileAsync(query.UserId, cancellationToken)
        ?? throw new UserNotFoundException();
}