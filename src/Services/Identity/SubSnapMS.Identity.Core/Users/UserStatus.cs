namespace SubSnapMS.Identity.Core.Users;

public enum UserStatus
{
    /// <summary>Può autenticarsi e usare la piattaforma.</summary>
    Active = 1,

    /// <summary>Disattivato (es. da un amministratore): non può autenticarsi.</summary>
    Deactivated = 2,

    /// <summary>Saga di cancellazione in corso: token revocati, nessun nuovo dato, in attesa delle conferme dei servizi.</summary>
    PendingDeletion = 3
}