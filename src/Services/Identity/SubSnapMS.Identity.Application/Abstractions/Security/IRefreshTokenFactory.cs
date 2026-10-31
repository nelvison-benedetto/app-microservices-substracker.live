namespace SubSnapMS.Identity.Application.Abstractions.Security;

/// <summary>Token in chiaro (solo per il client) e suo hash (solo per il database).</summary>
public sealed record GeneratedRefreshToken(string Token, string Hash)
{
    public override string ToString() => $"GeneratedRefreshToken {{ Hash = {Hash} }}";
}

public interface IRefreshTokenFactory
{
    /// <summary>Token crittograficamente casuale + hash.</summary>
    GeneratedRefreshToken Generate();

    /// <summary>Hash deterministico (SHA-256) di un token ricevuto dal client, per il lookup.</summary>
    string ComputeHash(string token);
}