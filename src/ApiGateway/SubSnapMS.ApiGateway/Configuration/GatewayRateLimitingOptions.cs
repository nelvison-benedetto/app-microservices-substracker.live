namespace SubSnapMS.ApiGateway.Configuration;

/// <summary>
/// Sezione "RateLimiting". I contatori sono in memoria, quindi valgono PER ISTANZA del gateway:
/// con N repliche imposta limiti divisi per N (solo configurazione, nessun cambio di codice).
/// </summary>
public sealed class GatewayRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    /// <summary>Limite generale: per utente se autenticato, per IP se anonimo.</summary>
    public FixedWindowSettings Global { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60 };

    /// <summary>Limite stretto per IP sulle route con "RateLimiterPolicy": "auth" (login, registrazione).</summary>
    public FixedWindowSettings Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}

public sealed class FixedWindowSettings
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}