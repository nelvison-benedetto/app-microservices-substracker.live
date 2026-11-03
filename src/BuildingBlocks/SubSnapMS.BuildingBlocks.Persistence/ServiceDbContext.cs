using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Application.Persistence;
using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.BuildingBlocks.Persistence.Converters;
using SubSnapMS.BuildingBlocks.Persistence.Exceptions;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SubSnapMS.BuildingBlocks.Persistence;

//Base del DbContext di ogni servizio. È anche l'IUnitOfWork della richiesta.
//A ogni SaveChangesAsync: esegue i domain event (stessa transazione), salva, traduce gli errori.
public abstract class ServiceDbContext : DbContext, IUnitOfWork
{
    /// <summary>Limite di sicurezza: handler che generano eventi all'infinito indicano un bug.</summary>
    private const int MaxDomainEventRounds = 10;  //limiti di giri dei domain event (se dopo 10 giri ci sono ancora eventi, c'è un loop infinito e si segnala un errore)

    private readonly IDomainEventDispatcher? _domainEventDispatcher;  //dispatcher, il componente di BuildingBlocks.Application che, dato un evento, trova ed esegue i suoi handler (IDomainEventHandler<T>)

    protected ServiceDbContext(DbContextOptions options, IDomainEventDispatcher? domainEventDispatcher)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureModel(modelBuilder);
        IgnoreDomainEvents(modelBuilder);
    }  //ricorda in EF il modello (tabelle, colonne, chiavi) si configura sovrascrivendo OnModelCreating, qu applichi anche sealed ovveride (quindi sigillato al 100%).


    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();  //ur converter x UTC, lo applichi ad ogni DateTimeOffset di ogni entity
    }  //convenzioni, sono regole generali che vuoi (ovviemante poi le tue config specifiche le possono sovrascrivere)


    private static void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        var aggregateTypes = modelBuilder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(clrType => typeof(IAggregateRoot).IsAssignableFrom(clrType))
            .ToList();

        foreach (var aggregateType in aggregateTypes)
            modelBuilder.Entity(aggregateType).Ignore(nameof(IAggregateRoot.DomainEvents));
    }  //ogni aggregate ha la proprietà pubblica DomainEvents, EF per convenzione, prova a mappare le proprietà pubbliche, ma questa è una collezione di interfacce, dunque non mappabile su una colonna. Bisognerebbe ricordarsi builder.Ignore(x => x.DomainEvents) in ogni configurazione di aggregate. Qui lo si fa una volta per tutti!


    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken);
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(exception);
        }
        catch (DbUpdateException exception) when (TranslateSaveException(exception) is { } translated)
        {
            throw translated;
        }
    }  //ogni SaveChangesAsync: prima esegue i domain event (stessa transazione), poi salva, poi traduce gli errori del db in eccezioni di dominio (es. indice univoco violato → 409). Se non sa tradurre, rilancia l'eccezione originale.


    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException(
            $"{GetType().Name}: use SaveChangesAsync. Synchronous saves would skip domain event handlers.");
    //dispatch dei domain event è asincrono (gli handler restituiscono Task). Un SaveChanges() sincrono non potrebbe eseguirli senza bloccare il thread, quindi li salterebbe: abbonamento salvato senza evento nell’Outbox, cioè un’incoerenza silenziosa. Meglio un errore esplicito appena qualcuno lo usa. Anche qui si sovrascrive l’overload con bool


    protected virtual Exception? TranslateSaveException(DbUpdateException exception) => null;

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        for (var round = 0; round < MaxDomainEventRounds; round++)
        {
            // Include anche gli aggregate in cancellazione (stato Deleted): i loro eventi vanno gestiti
            var aggregates = ChangeTracker.Entries<IAggregateRoot>()
                .Select(entry => entry.Entity)
                .Where(aggregate => aggregate.DomainEvents.Count > 0)
                .ToList();

            if (aggregates.Count == 0)
                return;

            if (_domainEventDispatcher is null)
                throw new InvalidOperationException(
                    $"{GetType().Name} has domain events to dispatch but no IDomainEventDispatcher was provided.");

            // Gli eventi vengono tolti PRIMA di eseguirli: un handler che modifica lo stesso aggregate
            // può generare nuovi eventi, che verranno gestiti nel giro successivo
            var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();
            foreach (var aggregate in aggregates)
                aggregate.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
                await _domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
        }

        throw new InvalidOperationException(
            $"{GetType().Name}: domain events are still being raised after {MaxDomainEventRounds} rounds. " +
            "A domain event handler is probably raising events in a loop.");
    }
    //traduce un errore del database in un'eccezione di dominio (es. indice univoco violato → 409).
    //restituisce null per rilanciare l'errore originale. Il servizio fa override perché il riconoscimento dipende dal provider (Postgres: SqlState 23505 + nome del vincolo).


}

//handler MediatR(es.ConfirmUploadHandler) modifica gli aggregate
//        │
//TransactionBehavior → unitOfWork.SaveChangesAsync() (unitOfWork = questo DbContext)
//        │
//ServiceDbContext.SaveChangesAsync
//  ├─ DispatchDomainEventsAsync
//  │    giro 1: MediaFile ha[Uploaded], il vecchio avatar ha[Deleted]
//  │            → svuota → handler → outbox.Add(media.uploaded.v1), outbox.Add(media.deleted.v1)
//  │    giro 2: nessun aggregate con eventi → return
//  │
//  ├─ base.SaveChangesAsync  → UPDATE media_files, DELETE vecchio avatar, INSERT 2 outbox_messages
//  │                           (una transazione)
//  │
//  └─ errori: concorrenza → 409 · violazione tradotta dal servizio → errore di dominio · altro → invariato

