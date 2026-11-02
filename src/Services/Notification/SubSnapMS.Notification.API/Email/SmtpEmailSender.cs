using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SubSnapMS.Notification.API.Configuration;

namespace SubSnapMS.Notification.API.Email;

/// <summary>
/// Invio via SMTP con MailKit: qualsiasi provider (Brevo, SES, Postmark, Resend…) o Mailpit in sviluppo.
/// Una connessione per email: con i volumi di SubSnap è semplice e sufficiente.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;
        mime.Headers.Add("Auto-Submitted", "auto-generated");   // niente risposte automatiche ("fuori ufficio")
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody, TextBody = message.TextBody }.ToMessageBody();

        using var client = new SmtpClient { Timeout = (int)settings.Smtp.Timeout.TotalMilliseconds };

        await client.ConnectAsync(settings.Smtp.Host, settings.Smtp.Port, settings.Smtp.Security, cancellationToken);

        if (!string.IsNullOrEmpty(settings.Smtp.Username))
            await client.AuthenticateAsync(settings.Smtp.Username, settings.Smtp.Password ?? string.Empty, cancellationToken);

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}