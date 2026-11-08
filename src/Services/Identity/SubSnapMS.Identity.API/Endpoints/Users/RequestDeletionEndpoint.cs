using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SubSnapMS.BuildingBlocks.Web.Endpoints;
using SubSnapMS.BuildingBlocks.Web.Responses;
using SubSnapMS.BuildingBlocks.Web.Security;
using SubSnapMS.Identity.Application.Features.Users.RequestDeletion;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.API.Endpoints.Users;

public sealed record RequestDeletionRequest(string CurrentPassword);

// 202 Accepted: la cancellazione è una saga asincrona (gli altri servizi cancellano i loro dati, poi Identity rimuove l'utente). Da subito l'account non può più autenticarsi.
internal sealed class RequestDeletionEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes) =>
        routes.MapDelete("users/me", HandleAsync)
            .WithTags(EndpointTags.Users)
            .WithName("DeleteMyAccount");

    //Su DELETE il body non viene dedotto in automatico: [FromBody] è obbligatorio
    private static async Task<Accepted> HandleAsync(
        [FromBody] RequestDeletionRequest request, ICurrentUser currentUser, ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(
            new RequestUserDeletionCommand(UserId.From(currentUser.UserId), request.CurrentPassword),
            cancellationToken);

        return ApiResponses.Accepted();
    }
}