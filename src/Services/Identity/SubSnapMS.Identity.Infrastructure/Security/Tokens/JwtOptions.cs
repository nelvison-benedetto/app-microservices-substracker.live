using SubSnapMS.Identity.Infrastructure.Security.Keys;

namespace SubSnapMS.Identity.Infrastructure.Security.Tokens;

//Sezione "Identity:Jwt". Issuer e Audience NON sono qui: arrivano da "Auth" (stessa fonte usata per validare), copiati in DependencyInjection.
public sealed class JwtOptions
{
    public const string SectionName = "Identity:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    //Durata dell'access token (allineata al cookie access_token del frontend)
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(10);

    //Chiavi RSA. Esattamente una Active (firma); le altre servono solo a validare durante la rotazione
    public List<SigningKeyOptions> SigningKeys { get; set; } = [];

    //Solo in sviluppo, se SigningKeys è vuoto: chiave generata e salvata qui (relativo alla content root)
    public string DevelopmentKeyPath { get; set; } = ".keys/identity-jwt-dev.pem";
}