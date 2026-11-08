using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.Subscriptions.Application.Common.Caching;
using SubSnapMS.Subscriptions.Application.Common.Results;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.Application.Features.Subscriptions.Create;

public sealed record CreateSubscriptionCommand(
    UserId UserId,
    string Name,
    string? Category,
    decimal Amount,
    string? Currency,  //ISO 4217 e.g.EUR, se null → valuta di default (EUR)
    string BillingCycle,  //weekly | monthly | quarterly | biannual | yearly
    DateOnly StartDate,
    DateOnly? EndDate) : ICommand<SubscriptionResult>, IInvalidatesUserSubscriptions;