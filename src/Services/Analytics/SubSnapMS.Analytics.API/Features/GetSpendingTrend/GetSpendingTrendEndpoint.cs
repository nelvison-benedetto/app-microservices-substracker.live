using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.Analytics.API.Calculations;
using SubSnapMS.Analytics.API.Persistence;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;

namespace SubSnapMS.Analytics.API.Features.GetSpendingTrend;

internal sealed class GetSpendingTrendEndpoint : IEndpoint
{
    private const int DefaultMonths = 12;
    private const int MaxMonths = 36;

    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("analytics/trend", HandleAsync)
            .WithTags(EndpointTags.Analytics)
            .WithName("GetSpendingTrend");

    private static async Task<Ok<ApiResult<SpendingTrendResponse>>> HandleAsync(
        int? months,
        ICurrentUser currentUser,
        SubscriptionReadStore store,
        AnalyticsCalendar calendar,
        CancellationToken cancellationToken)
    {
        var monthCount = months ?? DefaultMonths;
        if (monthCount is < 1 or > MaxMonths)
            throw new DomainException("analytics.months_invalid", $"months must be between 1 and {MaxMonths}.");

        var subscriptions = await store.GetForUserAsync(currentUser.UserId, cancellationToken);
        return ApiResponses.Ok(TrendCalculator.Build(subscriptions, calendar.Today(), monthCount));
    }
}