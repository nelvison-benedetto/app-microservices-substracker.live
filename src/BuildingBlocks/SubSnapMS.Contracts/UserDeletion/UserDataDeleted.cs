using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts.UserDeletion;

/// <summary>
/// Risposta alla saga di cancellazione: "il servizio X ha cancellato tutti i dati dell'utente".
/// La pubblica ogni servizio che possiede dati (Subscriptions, Media, ...);
/// la consuma solo l'orchestratore in Identity, che attende tutte le conferme attese.
/// </summary>
[IntegrationEvent(Exchanges.UserDeletion, "user-data.deleted.v1")]
public sealed record UserDataDeleted : IntegrationEvent
{
    public required Guid UserId { get; init; }

    /// <summary>Nome del servizio che conferma (stesso valore di serviceName in AddMessaging).</summary>
    public required string Service { get; init; }

    public required DateTimeOffset DeletedOnUtc { get; init; }
}
