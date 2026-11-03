using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.Contracts.Identity;

//E' UN EVENTO CONCRETO, un utente si è registrato.
//questo evento concreto verra poi consumato da service e.g. Notification (email di benvenuto, copia locale dell'email), Graph (nodo User).
/// 
[IntegrationEvent(Exchanges.Identity, "user.registered.v1")]  //questo attributo dice che questo evento, sul broker, si pubblica sull’exchange identity.events e si chiama user.registered.v1
public sealed record UserRegistered : IntegrationEvent
{

    public required Guid UserId { get; init; }

    public required string Email { get; init; }  //email normalizzata in lowercase

    public required DateTimeOffset RegisteredOnUtc { get; init; }

}

