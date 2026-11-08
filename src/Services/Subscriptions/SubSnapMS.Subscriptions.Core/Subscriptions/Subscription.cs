using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Subscriptions.Core.Common;
using SubSnapMS.Subscriptions.Core.Subscriptions.Events;
using SubSnapMS.Subscriptions.Core.Subscriptions.Exceptions;

namespace SubSnapMS.Subscriptions.Core.Subscriptions;

//Aggregate root dell'abbonamento. Si referenzia all'utente solo per Id.
//Lo storico è append-only: l'aggregate aggiunge voci ma non carica mai quelle passate
public sealed class Subscription : AggregateRoot<SubscriptionId>
{
    private readonly List<SubscriptionHistoryEntry> _historyEntries = [];
    private bool _deleted;

    private Subscription() { }   //x EF Core

    public UserId UserId { get; private set; }
    public SubscriptionDetails Details { get; private set; } = default!;

    //1 alla creazione, +1 a ogni modifica effettiva. Concorrenza ottimistica e ordine degli eventi
    public long Version { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    //costo mensile equivalente (stessa valuta), calcolato dal ciclo di fatturazione
    public Money MonthlyCost => Details.Price.ToMonthly(Details.BillingCycle);

    //Solo le voci aggiunte in QUESTA operazione: il repository non carica lo storico esistente (si legge dal lato query). Le nuove voci vengono inserite al salvataggio
    public IReadOnlyCollection<SubscriptionHistoryEntry> HistoryEntries => _historyEntries.AsReadOnly();



    public static Subscription Create(UserId userId, SubscriptionDetails details, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(details);

        var subscription = new Subscription
        {
            Id = SubscriptionId.New(),
            UserId = userId,
            Details = details,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        subscription._historyEntries.Add(
            SubscriptionHistoryEntry.Record(SubscriptionHistoryAction.Created, details.DescribeAll(), now));

        subscription.Raise(new SubscriptionCreatedDomainEvent(subscription.Id, userId, details, subscription.Version, now));
        return subscription;
    }

    //Sostituisce i dettagli. Restituisce false se non cambia nulla (niente storico, niente evento, version invariata)
    public bool Update(SubscriptionDetails details, long? expectedVersion, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (expectedVersion is { } expected && expected != Version)
            throw new SubscriptionVersionMismatchException();

        var changes = details.DiffFrom(Details);
        if (changes.Count == 0)
            return false;

        Details = details;
        Version++;
        UpdatedAtUtc = now;

        _historyEntries.Add(SubscriptionHistoryEntry.Record(SubscriptionHistoryAction.Updated, changes, now));
        Raise(new SubscriptionUpdatedDomainEvent(Id, UserId, details, Version, now));
        return true;
    }

    //Registra la cancellazione (evento per i read model). La rimozione la fa il repository; lo storico viene eliminato in cascade dal database.
    public void Delete(DateTimeOffset now)
    {
        if (_deleted)
            return;

        _deleted = true;
        Raise(new SubscriptionDeletedDomainEvent(Id, UserId, now));
    }

    //un abbonamento di un altro utente è trattato come inesistente (404): non se ne rivela l'esistenza
    public void EnsureOwnedBy(UserId userId)
    {
        if (UserId != userId)
            throw new SubscriptionNotFoundException();
    }
}