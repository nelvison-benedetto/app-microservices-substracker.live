using MediatR;
using SubSnapMS.BuildingBlocks.Application.Messaging;
using SubSnapMS.BuildingBlocks.Application.Persistence;

namespace SubSnapMS.BuildingBlocks.Application.Behaviors;

//Confine transazionale di ogni command: l'handler modifica gli aggregate in memoria, qui si salva tutto con UN SaveChanges (aggregate + domain event handler + Outbox).
//Se l'handler lancia un'eccezione non si salva nulla. Le query passano senza salvataggio.
internal sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is not ICommandBase)  //vede se la richiesta è un command (va salvato!) o una query (sola lettura, niente da salvare)
            return await next();
        var response = await next();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}