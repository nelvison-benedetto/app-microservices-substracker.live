using SubSnapMS.Notification.API.Diagnostics;
using SubSnapMS.Notification.API.Email;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.Delivery;

public enum SendOutcome
{
    Sent,
    AlreadySent,
    GaveUp
}

//Un altro tentativo sta inviando la stessa notifica: il chiamante riprova più tardi
public sealed class NotificationInProgressException(string key)
    : Exception($"Notification '{key}' is being sent by another attempt.");

//Invio "una volta sola": prenota la chiave → invia → segna come inviata.
//Se l'invio fallisce la notifica resta Failed e il chiamante decide (consumer: retry e DLQ; job: giro successivo).
public sealed class NotificationSender(
    NotificationLog log,
    IEmailSender emailSender,
    TimeProvider timeProvider,
    ILogger<NotificationSender> logger)
{
    public async Task<SendOutcome> SendOnceAsync(
        string key,
        Guid userId,
        string type,
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        using var activity = NotificationTelemetry.ActivitySource.StartActivity("notification.send");
        activity?.SetTag("notification.type", type);
        activity?.SetTag("notification.key", key);

        var claim = await log.TryClaimAsync(key, userId, type, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        switch (claim)
        {
            case ClaimResult.AlreadySent:
                NotificationTelemetry.RecordEmail(type, "skipped");
                return SendOutcome.AlreadySent;

            case ClaimResult.InProgress:
                throw new NotificationInProgressException(key);

            case ClaimResult.GaveUp:
                logger.LogWarning("Notification {Key} not sent: attempts exhausted", key);
                NotificationTelemetry.RecordEmail(type, "skipped");
                return SendOutcome.GaveUp;
        }

        try
        {
            await emailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            // CancellationToken.None: lo stato Failed va scritto anche durante uno shutdown
            await log.MarkFailedAsync(key, exception.Message, CancellationToken.None);
            NotificationTelemetry.RecordEmail(type, "failed");
            throw;
        }

        // Email già consegnata all'SMTP: lo stato va scritto anche durante uno shutdown
        await log.MarkSentAsync(key, timeProvider.GetUtcNow().UtcDateTime, CancellationToken.None);
        NotificationTelemetry.RecordEmail(type, "sent");

        logger.LogInformation("Notification {Key} sent", key);   // niente email nei log: solo la chiave
        return SendOutcome.Sent;
    }
}