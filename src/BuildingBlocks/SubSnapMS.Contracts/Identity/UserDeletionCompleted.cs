using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Identity;

//Fine della Saga: tutti i servizi hanno confermato e Identity ha cancellato definitivamente l'utente, utile per audit e per le pulizie finali (es. Notification rimuove la copia dell'email stored).

[IntegrationEvent(Exchanges.Identity, "user.deletion-completed.v1")]
public sealed record UserDeletionCompleted : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required DateTimeOffset CompletedOnUtc { get; init; }
}
