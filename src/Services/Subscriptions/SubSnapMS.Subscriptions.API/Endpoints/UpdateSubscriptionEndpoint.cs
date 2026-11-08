using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Subscriptions.API.Endpoints.Common;
using SubSnapMS.Subscriptions.Application.Features.Subscriptions.Update;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions;

namespace SubSnapMS.Subscriptions.API.Endpoints;

//Opzionale: la version letta dal client. Se nel frattempo l'abbonamento è stato modificato (version diversa) la richiesta fallisce con 409 invece di sovrascrivere le modifiche altrui.
public sealed record UpdateSubscriptionRequest(
    string Name,
    string? Category,
    decimal Amount,
    string? Currency,
    string BillingCycle,
    DateOnly StartDate,
    DateOnly? EndDate,
    long? Version);

internal sealed class UpdateSubscriptionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapPut("subscriptions/{id:guid}", HandleAsync)
            .WithTags(EndpointTags.Subscriptions)
            .WithName("UpdateSubscription");

    private static async Task<Ok<ApiResult<SubscriptionResponse>>> HandleAsync(
        Guid id, UpdateSubscriptionRequest request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateSubscriptionCommand(
            UserId.From(currentUser.UserId),
            SubscriptionId.From(id),
            request.Name,
            request.Category,
            request.Amount,
            request.Currency,
            request.BillingCycle,
            request.StartDate,
            request.EndDate,
            request.Version), cancellationToken);

        return ApiResponses.Ok(SubscriptionResponse.From(result));
    }
}