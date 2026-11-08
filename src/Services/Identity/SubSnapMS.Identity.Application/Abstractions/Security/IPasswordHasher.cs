namespace SubSnapMS.Identity.Application.Abstractions.Security;

public enum PasswordVerification
{
    Failed = 0,
    Success = 1,

    //Password corretta, ma l'hash usa parametri superati: va ricalcolato
    SuccessRehashNeeded = 2
}

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string passwordHash, string password);

    //Esegue una verifica su un hash fittizio, con lo stesso costo di una vera.
    //Usata quando l'email non esiste: i tempi di risposta non rivelano quali email sono registrate.
    void VerifyDummy(string password);
}