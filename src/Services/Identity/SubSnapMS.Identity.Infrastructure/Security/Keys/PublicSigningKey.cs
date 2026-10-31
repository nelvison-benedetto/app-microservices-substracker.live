namespace SubSnapMS.Identity.Infrastructure.Security.Keys;

/// <summary>Chiave pubblica RSA nel formato JWK: modulus (n) ed exponent (e) in base64url.</summary>
public sealed record PublicSigningKey(string KeyId, string Modulus, string Exponent);