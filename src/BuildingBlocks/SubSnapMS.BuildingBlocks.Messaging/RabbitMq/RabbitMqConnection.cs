using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

/// <summary>
/// UNA connessione TCP per processo (raccomandazione RabbitMQ), condivisa da publisher e consumer.
/// Su questa connessione si aprono più channel leggeri.
/// </summary>
internal sealed class RabbitMqConnection(
    IOptions<RabbitMqOptions> options,
    MessagingSettings settings,
    ILogger<RabbitMqConnection> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        // Con l'automatic recovery l'oggetto connessione resta lo stesso anche dopo una caduta di rete:
        // RabbitMQ.Client la ricollega da solo (insieme a exchange, code, binding e consumer).
        if (_connection is not null) return _connection;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null) return _connection;

            var rabbit = options.Value;
            var factory = new ConnectionFactory
            {
                ClientProvidedName = $"{settings.ServiceName}@{Environment.MachineName}", // visibile nella management UI
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(10),
                ConsumerDispatchConcurrency = 1   // un messaggio alla volta per channel: ordine preservato
            };

            if (rabbit.Uri is not null)
            {
                factory.Uri = rabbit.Uri;
            }
            else
            {
                factory.HostName = rabbit.Host;
                factory.Port = rabbit.Port;
                factory.UserName = rabbit.Username;
                factory.Password = rabbit.Password;
                factory.VirtualHost = rabbit.VirtualHost;
            }

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}{VirtualHost}", factory.HostName, factory.Port, factory.VirtualHost);

            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();

        _lock.Dispose();
    }
}