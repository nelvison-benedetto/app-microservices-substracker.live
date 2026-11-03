using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.UserDeletion;

//Risposta alla Saga(flusso strutturato di eventi) di cancellazione: "il servizio X ha cancellato tutti i dati dell'utente".
//La pubblica ogni servizio che possiede dati (Subscriptions, Media), la consuma solo l'orchestratore in Identity, che attende tutte le conferme attese.
[IntegrationEvent(Exchanges.UserDeletion, "user-data.deleted.v1")]
public sealed record UserDataDeleted : IntegrationEvent
{
    public required Guid UserId { get; init; }

    public required string Service { get; init; }  //service (e.g.Notification) che conferma

    public required DateTimeOffset DeletedOnUtc { get; init; }

}