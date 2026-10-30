using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.Contracts;

/// <summary>
/// Nomi ufficiali dei servizi della piattaforma. Usali ovunque serva il nome di un servizio:
///  - AddMessaging(serviceName: ...)  → nomi di coda e DLQ ("subscriptions.queue");
///  - AddObservability(serviceName: ...) → nome nelle trace;
///  - campo Service di UserDataDeleted e lista dei partecipanti alla saga in Identity.
/// Devono coincidere con i nomi dei servizi nel docker-compose (DNS interno).
/// </summary>
public static class ServiceNames
{
    public const string ApiGateway = "api-gateway";
    public const string Identity = "identity";
    public const string Subscriptions = "subscriptions";
    public const string Media = "media";
    public const string Notification = "notification";
    public const string Analytics = "analytics";
    public const string Search = "search";
    public const string Graph = "graph";
}