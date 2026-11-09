using Microsoft.Extensions.Options;
using SubSnapMS.Notification.API.Configuration;
using SubSnapMS.Notification.API.Delivery;
using SubSnapMS.Notification.API.Email;
using SubSnapMS.Notification.API.Persistence;

namespace SubSnapMS.Notification.API.Renewals;

//Ogni CheckInterval, nella fascia oraria consentita:
// 1. porta avanti le date di rinnovo superate;
// 2. invia i promemoria per i rinnovi tra oggi e oggi + DaysBefore.
//Con più repliche gira su tutte: il registro invii impedisce i doppioni, nessun lock necessario.
internal sealed class RenewalReminderService(
    SubscriptionStore subscriptions,
    RecipientStore recipients,
    NotificationSender sender,
    EmailTemplates templates,
    LocalCalendar calendar,
    IOptions<NotificationOptions> options,
    TimeProvider timeProvider,
    ILogger<RenewalReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value.Reminders;

        if (!settings.Enabled)
        {
            logger.LogInformation("Renewal reminders disabled");
            return;
        }

        using var timer = new PeriodicTimer(settings.CheckInterval, timeProvider);

        try
        {
            do
            {
                try
                {
                    await RunOnceAsync(settings, stoppingToken);
                }
                catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(exception, "Renewal reminder run failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // shutdown normale
        }
    }

    private async Task RunOnceAsync(ReminderOptions settings, CancellationToken cancellationToken)
    {
        var now = calendar.Now();
        if (now.Hour < settings.SendFromHour || now.Hour >= settings.SendUntilHour)
            return;

        var today = DateOnly.FromDateTime(now.DateTime);

        int advanced;
        do
        {
            advanced = await subscriptions.AdvanceStaleRenewalsAsync(today, settings.BatchSize, cancellationToken);
        }
        while (advanced == settings.BatchSize);

        var due = await subscriptions.FindDueRemindersAsync(today, today.AddDays(settings.DaysBefore), settings.BatchSize, cancellationToken);
        if (due.Count == 0)
            return;

        var emails = await recipients.GetEmailsAsync(due.Select(s => s.UserId).Distinct(), cancellationToken);
        var sent = 0;

        foreach (var subscription in due)
        {
            var renewalDate = subscription.NextRenewalDate!.Value.ToDateOnly();

            //Email non (ancora) nota: riprova al prossimo giro
            if (!emails.TryGetValue(subscription.UserId, out var email))
                continue;

            try
            {
                var message = templates.RenewalReminder(email, new RenewalReminderModel(
                    subscription.Name,
                    subscription.Amount,
                    subscription.Currency,
                    subscription.BillingCycle,
                    renewalDate), today);

                var outcome = await sender.SendOnceAsync(
                    NotificationKeys.RenewalReminder(subscription.SubscriptionId, renewalDate),
                    subscription.UserId,
                    NotificationTypes.RenewalReminder,
                    message,
                    cancellationToken);

                await subscriptions.MarkReminderHandledAsync(subscription.SubscriptionId, renewalDate, cancellationToken);

                if (outcome == SendOutcome.Sent)
                    sent++;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                //Resta ReminderPending: riprovato al prossimo giro (fino a MaxAttempts)
                logger.LogWarning(exception, "Renewal reminder for subscription {SubscriptionId} failed", subscription.SubscriptionId);
            }
        }

        logger.LogInformation("Renewal reminders: {Sent} sent, {Due} due", sent, due.Count);
    }
}