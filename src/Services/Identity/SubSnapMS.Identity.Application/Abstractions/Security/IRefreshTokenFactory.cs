namespace SubSnapMS.Identity.Application.Abstractions.Security;

//Token in chiaro (solo per il client) e suo hash (solo per il database)
public sealed record GeneratedRefreshToken(string Token, string Hash)
{
    public override string ToString() => $"GeneratedRefreshToken {{ Hash = {Hash} }}";
}

public interface IRefreshTokenFactory
{
    //Token crittograficamente casuale + hash
    GeneratedRefreshToken Generate();

    //Hash deterministico (SHA-256) di un token ricevuto dal client, per il lookup
    string ComputeHash(string token);
}