using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Consuming;
using SubSnapMS.BuildingBlocks.Messaging.Diagnostics;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// Dichiara la topologia del servizio (coda quorum + DLQ + binding) e consuma i messaggi.
/// ack = processato · nack senza requeue = va in DLQ · nessuna risposta (shutdown) = RabbitMQ lo riconsegna.
/// </summary>
internal sealed class RabbitMqConsumerService(
    RabbitMqConnection connection,
    ConsumerRegistry registry,
    MessageDispatcher dispatcher,
    MessagingSettings settings,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerService> logger) : BackgroundService
{
    private static readonly TimeSpan StartupRetryDelay = TimeSpan.FromSeconds(5);
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // RabbitMQ potrebbe non essere ancora pronto all'avvio: riprova finché non si collega
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartConsumingAsync(stoppingToken);
                break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not start the RabbitMQ consumer. Retrying in {Delay}", StartupRetryDelay);
                await DisposeChannelAsync();
                await Task.Delay(StartupRetryDelay, stoppingToken);
            }
        }

        // I messaggi arrivano sul channel tramite l'evento ReceivedAsync: qui si resta solo in attesa dello stop
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task StartConsumingAsync(CancellationToken stoppingToken)
    {
        var rabbitConnection = await connection.GetConnectionAsync(stoppingToken);
        var channel = await rabbitConnection.CreateChannelAsync(cancellationToken: stoppingToken);
        _channel = channel;

        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: options.Value.PrefetchCount, global: false, cancellationToken: stoppingToken);

        // Dead letter: exchange fanout + coda DLQ del servizio
        await channel.ExchangeDeclareAsync(settings.DeadLetterExchange, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            queue: settings.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(settings.DeadLetterQueue, settings.DeadLetterExchange, routingKey: string.Empty, cancellationToken: stoppingToken);

        // Coda principale (quorum = replicata e durevole); i messaggi rifiutati vanno nel DLX
        await channel.QueueDeclareAsync(
            queue: settings.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-queue-type"] = "quorum",
                ["x-dead-letter-exchange"] = settings.DeadLetterExchange
            },
            cancellationToken: stoppingToken);

        // Un binding per ogni evento a cui il servizio è iscritto
        foreach (var registration in registry.Registrations)
        {
            await channel.ExchangeDeclareAsync(registration.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: stoppingToken);
            await channel.QueueBindAsync(settings.QueueName, registration.Exchange, routingKey: registration.EventName, cancellationToken: stoppingToken);
        }

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => OnMessageAsync(channel, delivery, stoppingToken);

        await channel.BasicConsumeAsync(queue: settings.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        logger.LogInformation(
            "Consuming {Queue} for events: {Events}",
            settings.QueueName,
            string.Join(", ", registry.Registrations.Select(r => r.EventName)));
    }

    private async Task OnMessageAsync(IChannel channel, BasicDeliverEventArgs delivery, CancellationToken stoppingToken)
    {
        var properties = delivery.BasicProperties;
        var eventName = properties.Type ?? MessageHeaders.GetString(properties.Headers, MessageHeaders.EventName);

        if (eventName is null || !registry.TryGet(eventName, out var registration))
        {
            // Non dovrebbe succedere (la coda riceve solo gli eventi legati), ma non deve bloccare la coda
            logger.LogWarning("Received message {MessageId} with unknown event '{EventName}': discarded", properties.MessageId, eventName);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);
            return;
        }

        IntegrationEvent? integrationEvent;
        try
        {
            integrationEvent = (IntegrationEvent?)JsonSerializer.Deserialize(delivery.Body.Span, registration.EventType, MessagingJson.Options);
        }
        catch (JsonException exception)
        {
            // Payload illeggibile: riprovare non serve, va direttamente in DLQ
            logger.LogError(exception, "Cannot deserialize {EventName} {MessageId}: moved to {Dlq}", eventName, properties.MessageId, settings.DeadLetterQueue);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            return;
        }

        if (integrationEvent is null)
        {
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            return;
        }

        var messageId = Guid.TryParse(properties.MessageId, out var parsedId) ? parsedId : integrationEvent.EventId;

        var parent = MessagingDiagnostics.ParseParent(
            MessageHeaders.GetString(properties.Headers, MessageHeaders.TraceParent),
            MessageHeaders.GetString(properties.Headers, MessageHeaders.TraceState));

        using var activity = MessagingDiagnostics.ActivitySource.StartActivity($"{eventName} process", ActivityKind.Consumer, parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.type", "process");
        activity?.SetTag("messaging.destination.name", settings.QueueName);
        activity?.SetTag("messaging.message.id", messageId.ToString());

        var context = new MessageContext(messageId, eventName, properties.AppId, delivery.Redelivered, Attempt: 1);

        try
        {
            await dispatcher.DispatchAsync(registration, integrationEvent, context, stoppingToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, CancellationToken.None);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutdown durante l'elaborazione: nessun ack, RabbitMQ lo riconsegnerà alla prossima istanza
            logger.LogInformation("Shutdown while processing {EventName} {MessageId}: it will be redelivered", eventName, messageId);
        }
        catch (Exception exception)
        {
            MessagingDiagnostics.RecordError(activity, exception);
            logger.LogError(exception, "{EventName} {MessageId} failed after all retries: moved to {Dlq}", eventName, messageId, settings.DeadLetterQueue);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);   // annulla ExecuteAsync e gli handler in corso
        await DisposeChannelAsync();               // i messaggi non confermati tornano in coda
    }

    private async Task DisposeChannelAsync()
    {
        if (_channel is null) return;

        try { await _channel.DisposeAsync(); }
        catch (Exception exception) { logger.LogDebug(exception, "Error while disposing the consumer channel"); }

        _channel = null;
    }
}
