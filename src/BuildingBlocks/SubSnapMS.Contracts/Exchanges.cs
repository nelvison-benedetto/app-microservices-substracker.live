using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts;

/// <summary>
/// Exchange RabbitMQ della piattaforma: uno per servizio produttore ("&lt;servizio&gt;.events").
/// Fa eccezione UserDeletion: è il canale delle risposte della saga di cancellazione account,
/// su cui pubblicano più servizi e che ascolta solo l'orchestratore in Identity.
/// </summary>
public static class Exchanges
{
    public const string Identity = "identity.events";
    public const string Subscriptions = "subscriptions.events";
    public const string Media = "media.events";
    public const string UserDeletion = "user-deletion.events";
}