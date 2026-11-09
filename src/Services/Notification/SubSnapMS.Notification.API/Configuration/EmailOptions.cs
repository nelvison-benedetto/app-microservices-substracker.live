using MailKit.Security;

namespace SubSnapMS.Notification.API.Configuration;

//Sezione "Email". Credenziali da variabili d'ambiente: Email__Smtp__Username, Email__Smtp__Password
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = string.Empty;

    public string FromName { get; set; } = "SubSnap";

    //Base dei link nelle email (es. https://substracker.live)
    public string AppBaseUrl { get; set; } = "https://substracker.live";

    public SmtpOptions Smtp { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    //None (Mailpit), StartTls (porta 587), SslOnConnect (porta 465), Auto
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}