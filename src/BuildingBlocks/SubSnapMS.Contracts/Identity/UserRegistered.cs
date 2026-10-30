using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.Identity;

/// <summary>
/// Un nuovo utente si è registrato.
/// Consumer tipici: Notification (email di benvenuto, copia locale dell'email), Graph (nodo User).
/// </summary>
[IntegrationEvent(Exchanges.Identity, "user.registered.v1")]
public sealed record UserRegistered : IntegrationEvent
{
    public required Guid UserId { get; init; }

    /// <summary>Email normalizzata (lowercase).</summary>
    public required string Email { get; init; }

    public required DateTimeOffset RegisteredOnUtc { get; init; }
}