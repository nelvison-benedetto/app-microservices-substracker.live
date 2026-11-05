namespace SubSnapMS.BuildingBlocks.Application.Persistence;

//Salva tutte le modifiche della richiesta in un'unica transazione, eseguendo prima i domain event.
//Lo chiama SOLO il TransactionBehavior (o l'Inbox per gli integration event), mai gli handler.
//Implementato dal DbContext del servizio (BuildingBlocks.Persistence → ServiceDbContext).

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}