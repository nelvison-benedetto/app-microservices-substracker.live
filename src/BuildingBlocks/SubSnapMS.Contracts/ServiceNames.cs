namespace SubSnapMS.Contracts;

//nome ufficiale di ogni service della piattaforma, deve coincidere con il nomi nel docker.
//ciascuno di questi finisce in ogni service, in code RabbitMQ, trace, Sentry, saga, DNS di Docker.

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


