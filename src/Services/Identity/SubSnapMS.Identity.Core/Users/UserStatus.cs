namespace SubSnapMS.Identity.Core.Users;

public enum UserStatus
{
    //Può autenticarsi e usare la piattaforma.</summary>
    Active = 1,

    //Disattivato (es. da un amministratore): non può autenticarsi
    Deactivated = 2,

    //Saga di cancellazione in corso: token revocati, nessun nuovo dato, in attesa delle conferme dei servizi
    PendingDeletion = 3
}