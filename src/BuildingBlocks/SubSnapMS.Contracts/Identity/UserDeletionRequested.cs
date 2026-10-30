using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Identity;

/// <summary>
/// Inizio della saga di cancellazione account (anche per richieste GDPR).
/// Identity ha già marcato l'utente come PendingDeletion e revocato i token.
/// Ogni servizio che POSSIEDE dati dell'utente (Subscriptions, Media) li cancella e risponde con UserDataDeleted.
/// I read model (Search, Analytics, Graph, Notification) cancellano e basta, senza rispondere.
/// </summary>
[IntegrationEvent(Exchanges.Identity, "user.deletion-requested.v1")]
public sealed record UserDeletionRequested : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required DateTimeOffset RequestedOnUtc { get; init; }
}