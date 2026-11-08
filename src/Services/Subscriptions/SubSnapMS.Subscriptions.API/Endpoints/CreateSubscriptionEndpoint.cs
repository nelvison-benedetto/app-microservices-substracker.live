using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Subscriptions.API.Endpoints.Common;
using SubSnapMS.Subscriptions.Application.Features.Subscriptions.Create;
using SubSnapMS.Subscriptions.Core.Common;

namespace SubSnapMS.Subscriptions.API.Endpoints;

public sealed record CreateSubscriptionRequest(
    string Name,
    string? Category,
    decimal Amount,
    string? Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate);

internal sealed class CreateSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPost("subscriptions", HandleAsync)
            .WithTags(EndpointTags.Subscriptions)
            .WithName("CreateSubscription");

    private static async Task<Created<ApiResult<SubscriptionResponse>>> HandleAsync(
        CreateSubscriptionRequest request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateSubscriptionCommand(
            UserId.From(currentUser.UserId),
            request.Name,
            request.Category,
            request.Amount,
            request.Currency,
            request.BillingCycle,
            request.StartDate,
            request.EndDate), cancellationToken);

        return ApiResponses.Created($"/api/v1/subscriptions/{result.Id}", SubscriptionResponse.From(result));
    }
}