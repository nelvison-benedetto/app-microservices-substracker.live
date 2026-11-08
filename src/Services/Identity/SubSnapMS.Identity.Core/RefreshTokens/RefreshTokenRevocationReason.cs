namespace SubSnapMS.Identity.Core.RefreshTokens;

public enum RefreshTokenRevocationReason
{
    //Sostituito da un nuovo token (refresh). Riusarlo dopo il periodo di tolleranza = furto
    Rotated = 1,

    Logout = 2,
    LogoutEverywhere = 3,
    PasswordChanged = 4,

    //Revocato perché un token della stessa famiglia è stato riusato
    ReuseDetected = 5,

    AccountDeletion = 6
}