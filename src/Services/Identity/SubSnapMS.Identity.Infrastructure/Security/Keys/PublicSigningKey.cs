namespace SubSnapMS.Identity.Infrastructure.Security.Keys;

//Chiave pubblica RSA nel formato JWK: modulus (n) ed exponent (e) in base64url
public sealed record PublicSigningKey(string KeyId, string Modulus, string Exponent);

