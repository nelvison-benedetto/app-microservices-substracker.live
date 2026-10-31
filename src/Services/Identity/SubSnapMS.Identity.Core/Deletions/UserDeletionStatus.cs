namespace SubSnapMS.Identity.Core.Deletions;

public enum UserDeletionStatus
{
    /// <summary>In attesa delle conferme dei servizi.</summary>
    InProgress = 1,

    /// <summary>Tutti i servizi hanno confermato e l'utente è stato rimosso.</summary>
    Completed = 2
}