using Microsoft.AspNetCore.Routing;

namespace SubSnapMS.BuildingBlocks.Web.Endpoints;

/// <summary>
/// Un endpoint per vertical slice (es. LoginEndpoint, RegisterUserEndpoint).
/// Sostituisce controller e Orchestrator del monolite: l'endpoint traduce HTTP → command → ISender.
/// Le classi che lo implementano vengono trovate e mappate in automatico.
/// </summary>
public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder routes);
}