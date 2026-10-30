using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Consuming;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.BuildingBlocks.Messaging.RabbitMq;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging;

public static class MessagingExtensions
{
    // Stesso valore di HealthCheckTags.Ready in Observability (le due librerie restano indipendenti)
    private const string ReadyTag = "ready";

    /// <summary>
    /// Registra RabbitMQ (publisher + consumer), e opzionalmente Outbox e Inbox su EF Core.
    /// </summary>
    /// <param name="serviceName">Nome del servizio: determina i nomi di coda e DLQ ("subscriptions.queue").</param>
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

        // Default: nessuna Inbox (handler idempotenti per costruzione). AddInbox<T>() lo sostituisce.
        services.TryAddScoped<IInboxStore, NullInboxStore>();

        if (registry.HasConsumers)
        {
            services.AddSingleton<MessageDispatcher>();
            services.AddHostedService<RabbitMqConsumerService>();
        }

        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: [ReadyTag]);

        return services;
    }
}