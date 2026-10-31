using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Common.Caching;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Delete;

public sealed record DeleteSubscriptionCommand(UserId UserId, SubscriptionId SubscriptionId)
    : ICommand, IInvalidatesUserSubscriptions;