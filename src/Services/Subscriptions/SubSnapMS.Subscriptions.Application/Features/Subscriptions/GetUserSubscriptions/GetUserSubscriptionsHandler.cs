using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Abstractions.Queries;
using SubSnapMS.Subscriptions.Application.Common.Caching;
using SubSnapMS.Subscriptions.Application.Common.Results;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.GetUserSubscriptions;

/// <summary>Cache-aside: dalla cache se presente, altrimenti dal database (e poi in cache).</summary>
internal sealed class GetUserSubscriptionsHandler(
    ISubscriptionQueries queries,
    UserSubscriptionsCache cache) : IQueryHandler<GetUserSubscriptionsQuery, IReadOnlyList<SubscriptionResult>>
{
    public async Task<IReadOnlyList<SubscriptionResult>> Handle(GetUserSubscriptionsQuery query, CancellationToken cancellationToken) =>
        await cache.GetOrLoadAsync(
            query.UserId,
            async token => await queries.GetUserSubscriptionsAsync(query.UserId, token),
            cancellationToken);
}