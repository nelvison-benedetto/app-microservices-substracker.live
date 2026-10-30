using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Identity;

/// <summary>
/// L'utente ha cambiato email. Chi tiene una copia dell'email (Notification) la aggiorna.
/// Non contiene la vecchia email: ai consumer non serve.
/// </summary>
[IntegrationEvent(Exchanges.Identity, "user.email-changed.v1")]
public sealed record UserEmailChanged : IntegrationEvent
{
    public required Guid UserId { get; init; }

    /// <summary>Nuova email normalizzata (lowercase).</summary>
    public required string NewEmail { get; init; }

    public required DateTimeOffset ChangedOnUtc { get; init; }
}