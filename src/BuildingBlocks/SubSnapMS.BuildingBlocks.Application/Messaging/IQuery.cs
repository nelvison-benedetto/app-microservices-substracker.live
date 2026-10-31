using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Messaging;

/// <summary>
/// Query: legge e basta. Nessun salvataggio, nessuna modifica di aggregate.
/// Di solito usa un port di lettura (proiezione diretta in DTO, senza tracking).
/// </summary>
public interface IQuery<out TResult> : IRequest<TResult>
{
}