namespace SubSnapMS.BuildingBlocks.Web.Authentication;

/// <summary>
/// Sezione "Auth" (stessi nomi del gateway). I servizi VERIFICANO i token, non li emettono.
/// </summary>
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Discovery di Identity sulla rete interna (es. http://identity:8080/.well-known/openid-configuration).
    /// Vuoto SOLO in Identity, che valida con le proprie chiavi senza chiamare se stesso via HTTP.
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>Valore atteso nel claim "iss".</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Valore atteso nel claim "aud".</summary>
    public string Audience { get; set; } = string.Empty;

    public int ClockSkewSeconds { get; set; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Configurazione 'Auth' incompleta: Issuer e Audience sono obbligatori.");
    }
}