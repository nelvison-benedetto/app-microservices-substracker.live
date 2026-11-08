namespace SubSnapMS.Identity.Infrastructure.Security.Keys;

//Una chiave RSA privata in formato PEM ("BEGIN PRIVATE KEY" o "BEGIN RSA PRIVATE KEY"), almeno 2048 bit (consigliati 3072).
//Inline (variabile d'ambiente) o da file (Docker secret). e.g.:
//   Identity__Jwt__SigningKeys__0__KeyId=2026-11
//   Identity__Jwt__SigningKeys__0__PrivateKeyPemFile=/run/secrets/identity_jwt_2026_11.pem
//   Identity__Jwt__SigningKeys__0__Active=true
public sealed class SigningKeyOptions
{
    //Identificativo univoco (finisce nell'header "kid" dei token e nel JWKS)
    public string KeyId { get; set; } = string.Empty;

    public string? PrivateKeyPem { get; set; }

    public string? PrivateKeyPemFile { get; set; }

    //True = firma i nuovi token. Esattamente una chiave attiva (o una sola chiave configurata)
    public bool Active { get; set; }
}