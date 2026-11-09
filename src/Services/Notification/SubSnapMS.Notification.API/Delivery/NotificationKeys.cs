using System.Globalization;

namespace SubSnapMS.Notification.API.Delivery;

public static class NotificationTypes
{
    public const string Welcome = "welcome";
    public const string RenewalReminder = "renewal-reminder";
}

//Chiavi di deduplica: identificano "questa notifica, per questo fatto". Stessa chiave = stessa email.
//Non cambiarne il formato senza pensarci: le notifiche già inviate verrebbero reinviate.
public static class NotificationKeys
{
    public static string Welcome(Guid userId) => $"{NotificationTypes.Welcome}:{userId}";

    //Un promemoria per ogni rinnovo: il mese dopo la chiave cambia e si invia di nuovo
    public static string RenewalReminder(Guid subscriptionId, DateOnly renewalDate) =>
        $"{NotificationTypes.RenewalReminder}:{subscriptionId}:{renewalDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
}