using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace SubSnapMS.BuildingBlocks.Messaging.Consuming;

/// <summary>Eventi a cui il servizio è iscritto e relativi handler. Costruito all'avvio, poi solo letto.</summary>
internal sealed class ConsumerRegistry
{
    private readonly Dictionary<string, EventRegistration> _registrations = new(StringComparer.Ordinal);

    public bool HasConsumers => _registrations.Count > 0;

    public IReadOnlyCollection<EventRegistration> Registrations => _registrations.Values;

    public void Add<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        var metadata = EventMetadata.For(typeof(TEvent));

        if (!_registrations.TryGetValue(metadata.Name, out var registration))
        {
            registration = new EventRegistration(metadata.Name, metadata.Exchange, typeof(TEvent));
            _registrations.Add(metadata.Name, registration);
        }
        else if (registration.EventType != typeof(TEvent))
        {
            throw new InvalidOperationException(
                $"L'evento '{metadata.Name}' è già associato al tipo {registration.EventType.FullName}.");
        }

        if (registration.Handlers.All(handler => handler.HandlerType != typeof(THandler)))
            registration.Handlers.Add(new HandlerInvoker<TEvent, THandler>());
    }

    public bool TryGet(string eventName, [NotNullWhen(true)] out EventRegistration? registration) =>
        _registrations.TryGetValue(eventName, out registration);
}

internal sealed class EventRegistration(string eventName, string exchange, Type eventType)
{
    public string EventName { get; } = eventName;
    public string Exchange { get; } = exchange;
    public Type EventType { get; } = eventType;
    public List<HandlerInvoker> Handlers { get; } = [];
}