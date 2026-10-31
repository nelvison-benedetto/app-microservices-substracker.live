using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Identity.Application.Abstractions.Queries;

namespace SubSnapMS.Identity.Application.Features.Users.GetStatus;

internal sealed class GetUserStatusHandler(IUserQueries queries) : IQueryHandler<GetUserStatusQuery, UserStatusResult>
{
    public async Task<UserStatusResult> Handle(GetUserStatusQuery query, CancellationToken cancellationToken) =>
        await queries.GetStatusAsync(query.UserId, cancellationToken) ?? UserStatusResult.NotFound;
}