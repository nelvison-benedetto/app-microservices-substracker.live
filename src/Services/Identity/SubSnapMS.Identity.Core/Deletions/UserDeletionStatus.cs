namespace SubSnapMS.Identity.Core.Deletions;

public enum UserDeletionStatus
{
    //In attesa delle conferme dei servizi
    InProgress = 1,

    //Tutti i servizi hanno confermato e l'utente è stato rimosso
    Completed = 2
}