namespace SubSnapMS.Notification.API.Email;

public interface IEmailSender
{
    //Lancia un'eccezione se il server SMTP non accetta il messaggio
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}