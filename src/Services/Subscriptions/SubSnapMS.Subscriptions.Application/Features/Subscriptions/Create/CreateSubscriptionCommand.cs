using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Common.Caching;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Create;

/// <param name="Currency">ISO 4217; null → valuta di default (EUR).</param>
/// <param name="BillingCycle">Codice: weekly | monthly | quarterly | biannual | yearly.</param>
public sealed record CreateSubscriptionCommand(
    UserId UserId,
    string Name,
    string? Category,
    decimal Amount,
    string? Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate) : ICommand<SubscriptionResult>, IInvalidatesUserSubscriptions;