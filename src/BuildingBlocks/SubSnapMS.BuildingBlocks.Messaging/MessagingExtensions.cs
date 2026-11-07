using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Consuming;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

namespace SubSnapMS.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    //Stesso valore di HealthCheckTags.Ready in Observability (le due librerie restano indipendenti)
    private const string ReadyTag = "ready";

    //Registra RabbitMQ (publisher + consumer) e, tramite il builder, Outbox e Inbox su EF Core.
    /// </summary>
    /// <param name="serviceName">Nome del servizio: determina coda e DLQ ("subscriptions.queue", "subscriptions.queue.dlq").</param>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,  //per leggere le sezioni Messaging:RabbitMq, Messaging:Outbox, Messaging:Inbox
        string serviceName,  //e.g.ServiceNames.Subscriptions , questo determina i nomi queues e DQL 
        Action<MessagingBuilder>? configure = null)  //facoltativa. lambda in cui dichiari Outbox, Inbox e consumer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);  //senza nome non si possono nominare le queues!

        //collega ogni classe di opzione alla sua sezione di configurazione
        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<InboxOptions>(configuration.GetSection(InboxOptions.SectionName));


        var registry = new ConsumerRegistry();  //registro vuoto x gli events consumati
        configure?.Invoke(new MessagingBuilder(services, registry));  //crea builder ed esegue la lambda

        services.AddSingleton(new MessagingSettings(serviceName));  //xk lo userà il consumer, il publisher e la connessione.
        services.AddSingleton(registry);  //registry diventa singleton nel DI

        //aggiungi anche questi al DI
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RabbitMqEventPublisher>());
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<RabbitMqEventPublisher>());

        services.TryAddScoped<IInboxStore, NullInboxStore>();  //se nella lambda hai chiamato AddInbox<T>(), EfCoreInboxStore è già registrato e questa riga non fa niente.

        if (registry.HasConsumers)  //il consumer viene registrato solo se la lambda ha dichiarato almeno 1 AddConsumer !
        {
            services.AddSingleton<MessageDispatcher>();
            services.AddHostedService<RabbitMqConsumerService>();
        }

        //aggiunge il check rabbitmq a /health/ready
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>(
            "rabbitmq",
            failureStatus: HealthStatus.Degraded,
            tags: [ReadyTag]);
        //se raabitmq non risponde, il check riporta Degraded & no Unhealthy
        //Degraded & no Unhealthy: con l'Outbox gli eventi aspettano nel DB, quindi un RabbitMQ giù non deve togliere il servizio dal traffico (es. il login deve continuare a funzionare).


        return services;
    }
}