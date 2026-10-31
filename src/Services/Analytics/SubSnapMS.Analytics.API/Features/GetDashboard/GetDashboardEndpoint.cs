using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.Analytics.API.Calculations;
using SubSnapMS.Analytics.API.Persistence;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;

namespace SubSnapMS.Analytics.API.Features.GetDashboard;

internal sealed class GetDashboardEndpoint : IEndpoint
{
    private const int DefaultUpcomingDays = 30;
    private const int MaxUpcomingDays = 90;

    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapGet("analytics/dashboard", HandleAsync)
            .WithTags(EndpointTags.Analytics)
            .WithName("GetDashboard");

    private static async Task<Ok<ApiResult<DashboardResponse>>> HandleAsync(
        int? upcomingDays,
        ICurrentUser currentUser,
        SubscriptionReadStore store,
        AnalyticsCalendar calendar,
        CancellationToken cancellationToken)
    {
        var days = upcomingDays ?? DefaultUpcomingDays;
        if (days is < 1 or > MaxUpcomingDays)
            throw new DomainException("analytics.upcoming_days_invalid", $"upcomingDays must be between 1 and {MaxUpcomingDays}.");

        var subscriptions = await store.GetForUserAsync(currentUser.UserId, cancellationToken);
        return ApiResponses.Ok(DashboardCalculator.Build(subscriptions, calendar.Today(), days));
    }
}