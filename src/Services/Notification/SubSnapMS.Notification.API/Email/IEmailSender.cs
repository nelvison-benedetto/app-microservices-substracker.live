namespace SubSnapMS.Notification.API.Email;

public interface IEmailSender
{
    /// <summary>Lancia un'eccezione se il server SMTP non accetta il messaggio.</summary>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}