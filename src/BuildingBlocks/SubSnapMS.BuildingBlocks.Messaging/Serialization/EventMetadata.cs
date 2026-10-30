using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Serialization;

/// <summary>Exchange e nome di un evento, letti (una volta, poi in cache) da [IntegrationEvent].</summary>
internal sealed record EventMetadata(string Exchange, string Name)
{
    private static readonly ConcurrentDictionary<Type, EventMetadata> Cache = new();

    public static EventMetadata For(Type eventType) => Cache.GetOrAdd(eventType, Resolve);

    private static EventMetadata Resolve(Type eventType)
    {
        var attribute = eventType.GetCustomAttribute<IntegrationEventAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"L'evento '{eventType.FullName}' non ha l'attributo [IntegrationEvent(exchange, name)].");

        if (string.IsNullOrWhiteSpace(attribute.Exchange) || string.IsNullOrWhiteSpace(attribute.Name))
            throw new InvalidOperationException($"L'evento '{eventType.FullName}' ha exchange o nome vuoti.");

        return new EventMetadata(attribute.Exchange, attribute.Name);
    }
}