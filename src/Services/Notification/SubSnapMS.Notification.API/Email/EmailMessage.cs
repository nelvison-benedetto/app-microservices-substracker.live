namespace SubSnapMS.Notification.API.Email;

//Email pronta da inviare: HTML più testo semplice (per i client senza HTML e per l'antispam)
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string TextBody);