namespace SubSnapMS.Media.API.Infrastructure.Storage;

/// <summary>
/// Sezione "Storage". Produzione (Cloudflare R2): AccountId + chiavi da variabili d'ambiente
/// (Storage__AccessKeyId, Storage__SecretAccessKey). Sviluppo (MinIO): ServiceUrl + ForcePathStyle.
/// </summary>
public sealed class ObjectStorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Account Cloudflare: l'endpoint diventa https://{AccountId}.r2.cloudflarestorage.com.</summary>
    public string? AccountId { get; set; }

    /// <summary>Endpoint esplicito (es. http://localhost:9000 per MinIO). Ha la precedenza su AccountId.</summary>
    public string? ServiceUrl { get; set; }

    public string BucketName { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    /// <summary>Base degli URL pubblici, es. https://cdn.substracker.live.</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;

    /// <summary>MinIO: true (bucket nel path). R2: false.</summary>
    public bool ForcePathStyle { get; set; }

    internal string ResolveServiceUrl() =>
        !string.IsNullOrWhiteSpace(ServiceUrl) ? ServiceUrl : $"https://{AccountId}.r2.cloudflarestorage.com";

    internal bool UsesHttps => ResolveServiceUrl().StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}