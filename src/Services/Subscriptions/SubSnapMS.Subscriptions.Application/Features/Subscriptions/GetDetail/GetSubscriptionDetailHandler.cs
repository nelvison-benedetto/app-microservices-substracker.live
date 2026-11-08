using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Abstractions.Queries;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Subscriptions.Exceptions;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.GetDetail;

//Il filtro per utente è nella query: un abbonamento altrui è indistinguibile da uno inesistente (404)
internal sealed class GetSubscriptionDetailHandler(ISubscriptionQueries queries)
    : IQueryHandler<GetSubscriptionDetailQuery, SubscriptionDetailResult>
{
    public async Task<SubscriptionDetailResult> Handle(GetSubscriptionDetailQuery query, CancellationToken cancellationToken) =>
        await queries.GetDetailAsync(query.UserId, query.SubscriptionId, cancellationToken)
        ?? throw new SubscriptionNotFoundException();
}