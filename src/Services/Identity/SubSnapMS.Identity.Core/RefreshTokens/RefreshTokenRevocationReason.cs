namespace SubSnapMS.Identity.Core.RefreshTokens;

public enum RefreshTokenRevocationReason
{
    /// <summary>Sostituito da un nuovo token (refresh). Riusarlo dopo il periodo di tolleranza = furto.</summary>
    Rotated = 1,

    Logout = 2,
    LogoutEverywhere = 3,
    PasswordChanged = 4,

    /// <summary>Revocato perché un token della stessa famiglia è stato riusato.</summary>
    ReuseDetected = 5,

    AccountDeletion = 6
}