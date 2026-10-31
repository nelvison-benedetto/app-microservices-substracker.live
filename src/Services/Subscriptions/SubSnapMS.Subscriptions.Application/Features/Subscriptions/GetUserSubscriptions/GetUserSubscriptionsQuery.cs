using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.GetUserSubscriptions;

public sealed record GetUserSubscriptionsQuery(UserId UserId) : IQuery<IReadOnlyList<SubscriptionResult>>;