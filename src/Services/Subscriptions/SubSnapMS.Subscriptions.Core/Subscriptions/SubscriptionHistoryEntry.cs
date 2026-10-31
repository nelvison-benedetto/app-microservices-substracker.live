using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.Subscriptions.Core.Subscriptions;

/// <summary>
/// Voce di storico, figlia di Subscription: si crea SOLO tramite l'aggregate e non si modifica mai.
/// Changes: i campi coinvolti (alla creazione tutti, con OldValue null; nelle modifiche solo quelli cambiati).
/// </summary>
public sealed class SubscriptionHistoryEntry : Entity<SubscriptionHistoryEntryId>
{
    private readonly List<SubscriptionFieldChange> _changes = [];

    private SubscriptionHistoryEntry() { }   // per EF Core

    public SubscriptionHistoryAction Action { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    public IReadOnlyList<SubscriptionFieldChange> Changes => _changes.AsReadOnly();

    internal static SubscriptionHistoryEntry Record(
        SubscriptionHistoryAction action,
        IEnumerable<SubscriptionFieldChange> changes,
        DateTimeOffset now)
    {
        var entry = new SubscriptionHistoryEntry
        {
            Id = SubscriptionHistoryEntryId.New(),
            Action = action,
            OccurredAtUtc = now
        };

        entry._changes.AddRange(changes);
        return entry;
    }
}