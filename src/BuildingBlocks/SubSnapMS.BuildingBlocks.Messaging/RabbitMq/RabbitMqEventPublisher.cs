using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Diagnostics;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

internal sealed class RabbitMqEventPublisher(
    RabbitMqConnection connection,
    MessagingSettings settings,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IMessagePublisher, IAsyncDisposable
{
    // Un channel non è thread-safe: le publish passano una alla volta
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly HashSet<string> _declaredExchanges = new(StringComparer.Ordinal);
    private IChannel? _channel;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();   // tipo reale, non quello dichiarato
        var metadata = EventMetadata.For(eventType);
        var current = Activity.Current;

        var message = new OutgoingMessage(
            integrationEvent.EventId,
            metadata.Exchange,
            metadata.Name,
            JsonSerializer.SerializeToUtf8Bytes(integrationEvent, eventType, MessagingJson.Options),
            integrationEvent.OccurredOnUtc,
            current?.Id,
            current?.TraceStateString);

        return PublishRawAsync(message, cancellationToken);
    }

    public async Task PublishRawAsync(OutgoingMessage message, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            await EnsureExchangeAsync(channel, message.Exchange, cancellationToken);

            var parent = MessagingDiagnostics.ParseParent(message.TraceParent, message.TraceState);
            using var activity = MessagingDiagnostics.ActivitySource.StartActivity(
                $"{message.EventName} publish", ActivityKind.Producer, parent);

            activity?.SetTag("messaging.system", "rabbitmq");
            activity?.SetTag("messaging.operation.type", "publish");
            activity?.SetTag("messaging.destination.name", message.Exchange);
            activity?.SetTag("messaging.rabbitmq.destination.routing_key", message.EventName);
            activity?.SetTag("messaging.message.id", message.MessageId.ToString());

            // Il consumer continuerà questa trace
            var headers = new Dictionary<string, object?>
            {
                [MessageHeaders.EventName] = message.EventName,
                [MessageHeaders.SourceService] = settings.ServiceName
            };

            var traceParent = activity?.Id ?? message.TraceParent;
            var traceState = activity?.TraceStateString ?? message.TraceState;
            if (traceParent is not null) headers[MessageHeaders.TraceParent] = traceParent;
            if (traceState is not null) headers[MessageHeaders.TraceState] = traceState;

            var properties = new BasicProperties
            {
                MessageId = message.MessageId.ToString(),
                Type = message.EventName,
                AppId = settings.ServiceName,
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,          // sopravvive a un riavvio del broker
                Timestamp = new AmqpTimestamp(message.OccurredOnUtc.ToUnixTimeSeconds()),
                Headers = headers
            };

            try
            {
                // Con i publisher confirms attivi, questa await termina solo quando RabbitMQ
                // ha preso in carico il messaggio; se lo rifiuta, lancia un'eccezione.
                // mandatory: false → un evento senza consumer iscritti viene scartato senza errore.
                await channel.BasicPublishAsync(
                    exchange: message.Exchange,
                    routingKey: message.EventName,
                    mandatory: false,
                    basicProperties: properties,
                    body: message.Body,
                    cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                MessagingDiagnostics.RecordError(activity, exception);
                throw;
            }

            MessagingDiagnostics.PublishedMessages.Add(1, new KeyValuePair<string, object?>("messaging.event.name", message.EventName));
            logger.LogDebug("Published {EventName} {MessageId} to {Exchange}", message.EventName, message.MessageId, message.Exchange);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Un errore a livello di channel lo chiude: alla prossima publish se ne apre uno nuovo
            await ResetChannelAsync();
            throw;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true }) return _channel;

        await ResetChannelAsync();

        var rabbitConnection = await connection.GetConnectionAsync(cancellationToken);
        _channel = await rabbitConnection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        return _channel;
    }

    private async Task EnsureExchangeAsync(IChannel channel, string exchange, CancellationToken cancellationToken)
    {
        if (_declaredExchanges.Contains(exchange)) return;

        // Idempotente: se l'exchange esiste già con gli stessi parametri non succede nulla
        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        _declaredExchanges.Add(exchange);
    }

    private async Task ResetChannelAsync()
    {
        _declaredExchanges.Clear();

        if (_channel is null) return;

        try { await _channel.DisposeAsync(); }
        catch (Exception exception) { logger.LogDebug(exception, "Error while disposing the publisher channel"); }

        _channel = null;
    }

    public async ValueTask DisposeAsync()
    {
        await ResetChannelAsync();
        _lock.Dispose();
    }
}