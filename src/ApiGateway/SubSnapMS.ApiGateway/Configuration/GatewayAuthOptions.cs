namespace SubSnapMS.ApiGateway.Configuration;

/// <summary>
/// Sezione "Auth". Il gateway NON firma token: li verifica con le chiavi pubbliche
/// che Identity pubblica sul suo documento di discovery (/.well-known/openid-configuration → jwks_uri).
/// </summary>
public sealed class GatewayAuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Discovery di Identity sulla rete interna, es. http://identity:8080/.well-known/openid-configuration</summary>
    public string MetadataAddress { get; set; } = string.Empty;

    /// <summary>Valore atteso nel claim "iss" (deve coincidere con quello che scrive Identity).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Valore atteso nel claim "aud".</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Tolleranza sugli orari di scadenza tra le macchine.</summary>
    public int ClockSkewSeconds { get; set; } = 30;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(MetadataAddress) || string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Configurazione 'Auth' incompleta: MetadataAddress, Issuer e Audience sono obbligatori.");
    }
}