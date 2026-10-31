namespace SubSnapMS.Identity.Infrastructure.Security.Keys;

/// <summary>
/// Una chiave RSA privata in formato PEM ("BEGIN PRIVATE KEY" o "BEGIN RSA PRIVATE KEY"), almeno 2048 bit (consigliati 3072).
/// Inline (variabile d'ambiente) o da file (Docker secret). Esempio:
///   Identity__Jwt__SigningKeys__0__KeyId=2026-11
///   Identity__Jwt__SigningKeys__0__PrivateKeyPemFile=/run/secrets/identity_jwt_2026_11.pem
///   Identity__Jwt__SigningKeys__0__Active=true
/// </summary>
public sealed class SigningKeyOptions
{
    /// <summary>Identificativo univoco (finisce nell'header "kid" dei token e nel JWKS).</summary>
    public string KeyId { get; set; } = string.Empty;

    public string? PrivateKeyPem { get; set; }

    public string? PrivateKeyPemFile { get; set; }

    /// <summary>True = firma i nuovi token. Esattamente una chiave attiva (o una sola chiave configurata).</summary>
    public bool Active { get; set; }
}