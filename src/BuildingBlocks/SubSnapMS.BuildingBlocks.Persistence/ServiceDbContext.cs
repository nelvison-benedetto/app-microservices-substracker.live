using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Application.DomainEvents;
using SubSnapMS.BuildingBlocks.Application.Persistence;
using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.BuildingBlocks.Persistence.Converters;
using SubSnapMS.BuildingBlocks.Persistence.Exceptions;

namespace SubSnapMS.BuildingBlocks.Persistence;

/// <summary>
/// Base del DbContext di ogni servizio. È anche l'IUnitOfWork della richiesta.
/// A ogni SaveChangesAsync: esegue i domain event (stessa transazione), salva, traduce gli errori.
/// </summary>
public abstract class ServiceDbContext : DbContext, IUnitOfWork
{
    /// <summary>Limite di sicurezza: handler che generano eventi all'infinito indicano un bug.</summary>
    private const int MaxDomainEventRounds = 10;

    private readonly IDomainEventDispatcher? _domainEventDispatcher;

    /// <param name="domainEventDispatcher">
    /// Null solo negli scenari senza domain event (es. strumenti di design-time).
    /// Se ci sono eventi da gestire e il dispatcher manca, il salvataggio fallisce invece di perderli.
    /// </param>
    protected ServiceDbContext(DbContextOptions options, IDomainEventDispatcher? domainEventDispatcher)
        : base(options)
    {
        _domainEventDispatcher = domainEventDispatcher;
    }

    // ── Modello ───────────────────────────────────────────────────────────────────

    /// <summary>Configurazione del modello del servizio (ApplyConfigurationsFromAssembly, Outbox, Inbox...).</summary>
    protected abstract void ConfigureModel(ModelBuilder modelBuilder);

    /// <summary>
    /// Sigillato: la base esegue SEMPRE la configurazione del servizio e poi le proprie regole,
    /// che devono vedere tutte le entità già registrate.
    /// </summary>
    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureModel(modelBuilder);
        IgnoreDomainEvents(modelBuilder);
    }

    /// <summary>
    /// Convenzioni valide per ogni servizio. Se il servizio fa override, deve chiamare base.ConfigureConventions.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Ogni DateTimeOffset (anche nullable) viene salvato e letto in UTC
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    private static void IgnoreDomainEvents(ModelBuilder modelBuilder)
    {
        var aggregateTypes = modelBuilder.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .Where(clrType => typeof(IAggregateRoot).IsAssignableFrom(clrType))
            .ToList();

        foreach (var aggregateType in aggregateTypes)
            modelBuilder.Entity(aggregateType).Ignore(nameof(IAggregateRoot.DomainEvents));
    }

    // ── Salvataggio ───────────────────────────────────────────────────────────────

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
    }

    /// <summary>
    /// Il salvataggio sincrono salterebbe i domain event (il dispatch è asincrono): non è supportato.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException(
            $"{GetType().Name}: use SaveChangesAsync. Synchronous saves would skip domain event handlers.");

    /// <summary>
    /// Traduce un errore del database in un'eccezione di dominio (es. indice univoco violato → 409).
    /// Restituisce null per rilanciare l'errore originale. Il servizio fa override perché il
    /// riconoscimento dipende dal provider (Postgres: SqlState 23505 + nome del vincolo).
    /// </summary>
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
}