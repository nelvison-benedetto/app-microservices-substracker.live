namespace SubSnapMS.Identity.Core.RefreshTokens;

public enum RefreshTokenRotationStatus
{
    Rotated = 1,

    //Token sconosciuto o sessione chiusa (logout, cambio password, account cancellato)
    Invalid = 2,

    Expired = 3,

    //Token già ruotato riusato fuori tolleranza: la famiglia è stata revocata (va salvato!)
    ReuseDetected = 4,

    UserNotActive = 5
}

//Esito della rotazione. NewToken è valorizzato solo se Status = Rotated
public sealed record RefreshTokenRotationResult(RefreshTokenRotationStatus Status, RefreshToken? NewToken)
{
    public bool Succeeded => Status == RefreshTokenRotationStatus.Rotated && NewToken is not null;

    internal static RefreshTokenRotationResult Success(RefreshToken newToken) =>
        new(RefreshTokenRotationStatus.Rotated, newToken);

    internal static RefreshTokenRotationResult Failure(RefreshTokenRotationStatus status) =>
        new(status, null);
}