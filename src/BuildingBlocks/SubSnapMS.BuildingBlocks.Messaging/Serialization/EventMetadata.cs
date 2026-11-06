using SubSnapMS.Contracts.Abstractions;
using System.Collections.Concurrent;
using System.Reflection;

namespace SubSnapMS.BuildingBlocks.Messaging.Serialization;

//è il collegamento tra Contracts e rabbitmq, usato sia da chi pubblica che da chi consuma.
//EventMetadata traduce un tipo C# di integration event nella sua identità sul broker: su quale exchange va e con quale nome viaggia. Legge l’attributo [IntegrationEvent(exchange, name)] con la reflection una volta sola per tipo, e poi TIENE IL RISULTATO IN CACHE.
//Exchange e nome di un evento, letti (una volta, poi in cache) da [IntegrationEvent]

internal sealed record EventMetadata(string Exchange, string Name)
{
    private static readonly ConcurrentDictionary<Type, EventMetadata> Cache = new();  //

    public static EventMetadata For(Type eventType) => Cache.GetOrAdd(eventType, Resolve);  //public, se la cache ha gia il tipo allora return i metadati salvati, altrimenti chiama Resolve(eventType) dunque salva il risultato e return it.

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