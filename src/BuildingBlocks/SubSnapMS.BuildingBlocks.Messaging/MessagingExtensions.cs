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
    // Stesso valore di HealthCheckTags.Ready in Observability (le due librerie restano indipendenti)
    private const string ReadyTag = "ready";

    /// <summary>
    /// Registra RabbitMQ (publisher + consumer) e, tramite il builder, Outbox e Inbox su EF Core.
    /// </summary>
    /// <param name="serviceName">Nome del servizio: determina coda e DLQ ("subscriptions.queue", "subscriptions.queue.dlq").</param>
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<MessagingBuilder>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);

        services.Configure<RabbitMqOptions>(configuration.GetSection(RabbitMqOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<InboxOptions>(configuration.GetSection(InboxOptions.SectionName));

        var registry = new ConsumerRegistry();
        configure?.Invoke(new MessagingBuilder(services, registry));

        services.AddSingleton(new MessagingSettings(serviceName));
        services.AddSingleton(registry);

        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<RabbitMqEventPublisher>();
        services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RabbitMqEventPublisher>());
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<RabbitMqEventPublisher>());

        // Default: nessuna Inbox (handler idempotenti per costruzione). AddInbox<T>() la sostituisce.
        services.TryAddScoped<IInboxStore, NullInboxStore>();

        if (registry.HasConsumers)
        {
            services.AddSingleton<MessageDispatcher>();
            services.AddHostedService<RabbitMqConsumerService>();
        }

        // Degraded e non Unhealthy: con l'Outbox gli eventi aspettano nel DB, quindi un RabbitMQ giù
        // non deve togliere il servizio dal traffico (es. il login deve continuare a funzionare).
        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>(
            "rabbitmq",
            failureStatus: HealthStatus.Degraded,
            tags: [ReadyTag]);

        return services;
    }
}