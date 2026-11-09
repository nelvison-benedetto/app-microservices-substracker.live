namespace SubSnapMS.Media.API.Infrastructure.Storage;

//Sezione "Storage". Produzione (Cloudflare R2): AccountId + chiavi da variabili d'ambiente (Storage__AccessKeyId, Storage__SecretAccessKey). Sviluppo (MinIO): ServiceUrl + ForcePathStyle.
public sealed class ObjectStorageOptions
{
    public const string SectionName = "Storage";

    //Account Cloudflare: l'endpoint diventa https://{AccountId}.r2.cloudflarestorage.com
    public string? AccountId { get; set; }

    //Endpoint esplicito (es. http://localhost:9000 per MinIO). Ha la precedenza su AccountId
    public string? ServiceUrl { get; set; }

    public string BucketName { get; set; } = string.Empty;

    public string AccessKeyId { get; set; } = string.Empty;

    public string SecretAccessKey { get; set; } = string.Empty;

    //Base degli URL pubblici, es. https://cdn.substracker.live
    public string PublicBaseUrl { get; set; } = string.Empty;

    //MinIO: true (bucket nel path). R2: false
    public bool ForcePathStyle { get; set; }

    internal string ResolveServiceUrl() =>
        !string.IsNullOrWhiteSpace(ServiceUrl) ? ServiceUrl : $"https://{AccountId}.r2.cloudflarestorage.com";

    internal bool UsesHttps => ResolveServiceUrl().StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}