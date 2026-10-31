using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Common.Caching;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Update;

/// <param name="ExpectedVersion">Version letta dal client: se diversa da quella attuale → 409 (modificato altrove).</param>
public sealed record UpdateSubscriptionCommand(
    UserId UserId,
    SubscriptionId SubscriptionId,
    string Name,
    string? Category,
    decimal Amount,
    string? Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate,
    long? ExpectedVersion) : ICommand<SubscriptionResult>, IInvalidatesUserSubscriptions;