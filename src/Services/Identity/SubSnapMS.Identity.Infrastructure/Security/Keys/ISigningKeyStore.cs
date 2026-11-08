using Microsoft.IdentityModel.Tokens;

namespace SubSnapMS.Identity.Infrastructure.Security.Keys;

//Chiavi RSA di Identity: una attiva per firmare, tutte valide per verificare (rotazione)
public interface ISigningKeyStore
{
    //Credenziali della chiave attiva (firma dei nuovi token)
    SigningCredentials GetSigningCredentials();

    //Chiavi pubbliche di TUTTE le chiavi configurate (validazione locale in Identity)
    IReadOnlyList<SecurityKey> GetValidationKeys();

    //Parametri pubblici per il JWKS (/.well-known/jwks.json)
    IReadOnlyList<PublicSigningKey> GetPublicKeys();
}