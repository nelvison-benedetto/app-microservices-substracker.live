using SubSnapMS.Identity.Infrastructure.Security.Keys;

namespace SubSnapMS.Identity.Infrastructure.Security.Tokens;

/// <summary>
/// Sezione "Identity:Jwt". Issuer e Audience NON sono qui: arrivano da "Auth" (stessa fonte usata
/// per validare), copiati in DependencyInjection.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Identity:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    /// <summary>Durata dell'access token (allineata al cookie access_token del frontend).</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Chiavi RSA. Esattamente una Active (firma); le altre servono solo a validare durante la rotazione.</summary>
    public List<SigningKeyOptions> SigningKeys { get; set; } = [];

    /// <summary>Solo in sviluppo, se SigningKeys è vuoto: chiave generata e salvata qui (relativo alla content root).</summary>
    public string DevelopmentKeyPath { get; set; } = ".keys/identity-jwt-dev.pem";
}