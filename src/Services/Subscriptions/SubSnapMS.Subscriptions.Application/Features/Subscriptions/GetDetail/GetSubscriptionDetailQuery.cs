using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.GetDetail;

public sealed record GetSubscriptionDetailQuery(UserId UserId, SubscriptionId SubscriptionId)
    : IQuery<SubscriptionDetailResult>;