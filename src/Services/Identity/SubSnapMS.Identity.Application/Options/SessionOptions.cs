namespace SubSnapMS.Identity.Application.Options;

//Sezione "Identity:Sessions"
public sealed class SessionOptions
{
    public const string SectionName = "Identity:Sessions";

    //Durata del refresh token (allineata al cookie refresh_token del frontend)
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(30);

    //Finestra in cui un token appena ruotato è ancora accettato (refresh paralleli dello stesso client)
    public TimeSpan ReuseGracePeriod { get; set; } = TimeSpan.FromSeconds(30);
}