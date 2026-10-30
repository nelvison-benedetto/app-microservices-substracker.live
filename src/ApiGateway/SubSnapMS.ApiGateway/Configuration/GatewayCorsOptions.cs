namespace SubSnapMS.ApiGateway.Configuration;

/// <summary>
/// Sezione "Cors". Vuota per default: Next.js chiama il gateway server-to-server e l'app mobile
/// non usa CORS. Serve solo se un domani un'app nel browser chiamerà direttamente api.substracker.live.
/// </summary>
public sealed class GatewayCorsOptions
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = [];

    public bool Enabled => AllowedOrigins.Length > 0;
}