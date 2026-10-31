using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.Identity.Core.Deletions.Events;
using SubSnapMS.Identity.Core.Users;

namespace SubSnapMS.Identity.Core.Deletions;

/// <summary>
/// Stato della saga di cancellazione di UN utente (Id = UserId). Aggregate separato da User,
/// perché sopravvive alla rimozione dell'utente come traccia di audit (senza dati personali).
/// ExpectedServices: i servizi che possiedono dati dell'utente e devono confermare con UserDataDeleted.
/// </summary>
public sealed class UserDeletion : AggregateRoot<UserId>
{
    private readonly List<string> _expectedServices = [];
    private readonly List<string> _confirmedServices = [];

    private UserDeletion() { }   // per EF Core

    public UserDeletionStatus Status { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public IReadOnlyCollection<string> ExpectedServices => _expectedServices.AsReadOnly();
    public IReadOnlyCollection<string> ConfirmedServices => _confirmedServices.AsReadOnly();

    public IReadOnlyCollection<string> PendingServices =>
        _expectedServices.Except(_confirmedServices, StringComparer.Ordinal).ToList().AsReadOnly();

    public bool AllServicesConfirmed => PendingServices.Count == 0;

    public static UserDeletion Start(UserId userId, IEnumerable<string> expectedServices, DateTimeOffset now)
    {
        var services = expectedServices
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var deletion = new UserDeletion
        {
            Id = userId,
            Status = UserDeletionStatus.InProgress,
            RequestedAtUtc = now
        };

        deletion._expectedServices.AddRange(services);
        return deletion;
    }

    /// <summary>
    /// Registra la conferma di un servizio. Idempotente: un duplicato (at-least-once) o un servizio
    /// non atteso restituiscono false senza errori.
    /// </summary>
    public bool Confirm(string service)
    {
        if (Status == UserDeletionStatus.Completed)
            return false;

        if (!_expectedServices.Contains(service, StringComparer.Ordinal) ||
            _confirmedServices.Contains(service, StringComparer.Ordinal))
            return false;

        _confirmedServices.Add(service);
        return true;
    }

    /// <summary>Chiude la saga. Da chiamare dopo aver rimosso l'utente, nella stessa transazione.</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status == UserDeletionStatus.Completed)
            return;

        if (!AllServicesConfirmed)
            throw new DomainException(
                "user_deletion.pending_services",
                $"Cannot complete the deletion: waiting for {string.Join(", ", PendingServices)}.");

        Status = UserDeletionStatus.Completed;
        CompletedAtUtc = now;
        Raise(new UserDeletionCompletedDomainEvent(Id, now));
    }
}