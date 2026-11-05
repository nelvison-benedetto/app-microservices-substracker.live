using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Messaging;

//Query (): legge e basta. Nessun salvataggio, nessuna modifica di aggregate.
//Di solito usa un port di lettura (proiezione diretta in DTO, senza tracking).
public interface IQuery<out TResult> : IRequest<TResult>  //IRequest è di mediatr 
{
}