using Microsoft.IdentityModel.Tokens;

namespace SubSnapMS.Identity.Infrastructure.Security.Keys;

/// <summary>Chiavi RSA di Identity: una attiva per firmare, tutte valide per verificare (rotazione).</summary>
public interface ISigningKeyStore
{
    /// <summary>Credenziali della chiave attiva (firma dei nuovi token).</summary>
    SigningCredentials GetSigningCredentials();

    /// <summary>Chiavi pubbliche di TUTTE le chiavi configurate (validazione locale in Identity).</summary>
    IReadOnlyList<SecurityKey> GetValidationKeys();

    /// <summary>Parametri pubblici per il JWKS (/.well-known/jwks.json).</summary>
    IReadOnlyList<PublicSigningKey> GetPublicKeys();
}