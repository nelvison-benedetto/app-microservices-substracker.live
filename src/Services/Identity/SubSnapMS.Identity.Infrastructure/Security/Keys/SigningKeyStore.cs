using System.Buffers.Text;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SubSnapMS.Identity.Infrastructure.Security.Tokens;

namespace SubSnapMS.Identity.Infrastructure.Security.Keys;

/// <summary>
/// Carica le chiavi all'avvio (singleton) e fallisce subito se la configurazione è sbagliata.
/// Produzione: chiavi da configurazione, obbligatorie. Sviluppo: se mancano, genera e salva una chiave locale.
/// </summary>
internal sealed class SigningKeyStore : ISigningKeyStore, IDisposable
{
    private const int MinimumKeySizeInBits = 2048;

    private readonly List<LoadedKey> _keys;
    private readonly SigningCredentials _signingCredentials;
    private readonly IReadOnlyList<SecurityKey> _validationKeys;
    private readonly IReadOnlyList<PublicSigningKey> _publicKeys;

    public SigningKeyStore(IOptions<JwtOptions> options, IHostEnvironment environment, ILogger<SigningKeyStore> logger)
    {
        var jwt = options.Value;

        _keys = jwt.SigningKeys.Count > 0
            ? jwt.SigningKeys.Select(LoadConfiguredKey).ToList()
            : environment.IsDevelopment()
                ? [LoadOrCreateDevelopmentKey(Path.Combine(environment.ContentRootPath, jwt.DevelopmentKeyPath), logger)]
                : throw new InvalidOperationException(
                    "No JWT signing keys configured (Identity:Jwt:SigningKeys). They are required outside Development.");

        var duplicate = _keys.GroupBy(k => k.KeyId, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate JWT signing key id '{duplicate.Key}'.");

        var activeKeys = _keys.Where(k => k.Active).ToList();
        var active = activeKeys.Count switch
        {
            1 => activeKeys[0],
            0 when _keys.Count == 1 => _keys[0],
            0 => throw new InvalidOperationException("No active JWT signing key: set Active=true on exactly one key."),
            _ => throw new InvalidOperationException("More than one active JWT signing key: exactly one must be Active.")
        };

        _signingCredentials = new SigningCredentials(
            new RsaSecurityKey(active.Rsa) { KeyId = active.KeyId },
            SecurityAlgorithms.RsaSha256);

        _validationKeys = _keys
            .Select(k => (SecurityKey)new RsaSecurityKey(k.Rsa.ExportParameters(includePrivateParameters: false)) { KeyId = k.KeyId })
            .ToList()
            .AsReadOnly();

        _publicKeys = _keys
            .Select(k =>
            {
                var parameters = k.Rsa.ExportParameters(includePrivateParameters: false);
                return new PublicSigningKey(
                    k.KeyId,
                    Base64Url.EncodeToString(parameters.Modulus!),
                    Base64Url.EncodeToString(parameters.Exponent!));
            })
            .ToList()
            .AsReadOnly();

        logger.LogInformation("JWT signing keys loaded: {KeyIds}. Active: {ActiveKeyId}",
            string.Join(", ", _keys.Select(k => k.KeyId)), active.KeyId);
    }

    public SigningCredentials GetSigningCredentials() => _signingCredentials;

    public IReadOnlyList<SecurityKey> GetValidationKeys() => _validationKeys;

    public IReadOnlyList<PublicSigningKey> GetPublicKeys() => _publicKeys;

    public void Dispose()
    {
        foreach (var key in _keys)
            key.Rsa.Dispose();
    }

    private static LoadedKey LoadConfiguredKey(SigningKeyOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.KeyId))
            throw new InvalidOperationException("Every JWT signing key needs a KeyId.");

        var pem = !string.IsNullOrWhiteSpace(options.PrivateKeyPem)
            ? options.PrivateKeyPem.Replace("\\n", "\n")   // PEM incollato in una variabile d'ambiente su una riga
            : !string.IsNullOrWhiteSpace(options.PrivateKeyPemFile)
                ? File.ReadAllText(options.PrivateKeyPemFile)
                : throw new InvalidOperationException($"JWT signing key '{options.KeyId}': set PrivateKeyPem or PrivateKeyPemFile.");

        return new LoadedKey(options.KeyId.Trim(), ImportRsa(pem, options.KeyId), options.Active);
    }

    private static LoadedKey LoadOrCreateDevelopmentKey(string path, ILogger logger)
    {
        RSA rsa;

        if (File.Exists(path))
        {
            rsa = ImportRsa(File.ReadAllText(path), "development");
        }
        else
        {
            rsa = RSA.Create(MinimumKeySizeInBits);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, rsa.ExportPkcs8PrivateKeyPem());
            logger.LogWarning("Generated a DEVELOPMENT JWT signing key at {Path}. Never use it in production.", path);
        }

        // Id stabile derivato dalla chiave pubblica: lo stesso file → lo stesso kid
        var thumbprint = Convert.ToHexStringLower(SHA256.HashData(rsa.ExportSubjectPublicKeyInfo()))[..12];
        return new LoadedKey($"dev-{thumbprint}", rsa, Active: true);
    }

    private static RSA ImportRsa(string pem, string keyId)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem);
        }
        catch (Exception exception) when (exception is ArgumentException or CryptographicException)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"JWT signing key '{keyId}' is not a valid RSA private key in PEM format.", exception);
        }

        if (rsa.KeySize < MinimumKeySizeInBits)
        {
            rsa.Dispose();
            throw new InvalidOperationException($"JWT signing key '{keyId}' must be at least {MinimumKeySizeInBits} bits.");
        }

        return rsa;
    }

    private sealed record LoadedKey(string KeyId, RSA Rsa, bool Active);
}