namespace SubSnapMS.Identity.Application.Options;

/// <summary>Sezione "Identity:Sessions".</summary>
public sealed class SessionOptions
{
    public const string SectionName = "Identity:Sessions";

    /// <summary>Durata del refresh token (allineata al cookie refresh_token del frontend).</summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    /// <summary>Finestra in cui un token appena ruotato è ancora accettato (refresh paralleli dello stesso client).</summary>
    public TimeSpan ReuseGracePeriod { get; set; } = TimeSpan.FromSeconds(30);
}