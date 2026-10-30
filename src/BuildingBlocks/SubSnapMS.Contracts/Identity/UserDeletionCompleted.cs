using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Identity;

/// <summary>
/// Fine della saga: tutti i servizi hanno confermato e Identity ha cancellato definitivamente l'utente.
/// Utile per audit e per eventuali pulizie finali (es. Notification rimuove la copia dell'email).
/// </summary>
[IntegrationEvent(Exchanges.Identity, "user.deletion-completed.v1")]
public sealed record UserDeletionCompleted : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required DateTimeOffset CompletedOnUtc { get; init; }
}
