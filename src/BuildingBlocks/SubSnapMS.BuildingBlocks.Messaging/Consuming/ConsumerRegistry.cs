using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System.Diagnostics.CodeAnalysis;

namespace SubSnapMS.BuildingBlocks.Messaging.Consuming;

//elenco degli eventi a cui un service è iscritto, con il loro handler.
internal sealed class ConsumerRegistry
{
    private readonly Dictionary<string, EventRegistration> _registrations = new(StringComparer.Ordinal);  //dizionario nome dell’evento → registrazione, StringComparer. Ordinal confronta char by char (case sensitive)

    public bool HasConsumers => _registrations.Count > 0;  //“Questo servizio consuma qualcosa?”

    public IReadOnlyCollection<EventRegistration> Registrations => _registrations.Values;  //tutte le registrazioni, in sola lettura. le usa il consumera all'avvio x creare i binding

    public void Add<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        var metadata = EventMetadata.For(typeof(TEvent));  //legge l’attributo [IntegrationEvent(exchange, name)] del tipo, con la reflection e una cache. Se l’evento non ha l’attributo, qui lancia un errore: il servizio non parte

        if (!_registrations.TryGetValue(metadata.Name, out var registration))
        {
            registration = new EventRegistration(metadata.Name, metadata.Exchange, typeof(TEvent));
            _registrations.Add(metadata.Name, registration);
        }  //se è la prima volta che si registra questo event, crea la sua EventRegistration (nome, exchange, tipo C#, lista di handler vuota) e la aggiunge al dictionary 
        else if (registration.EventType != typeof(TEvent))
        {
            throw new InvalidOperationException(
                $"L'evento '{metadata.Name}' è già associato al tipo {registration.EventType.FullName}.");
        }

        if (registration.Handlers.All(handler => handler.HandlerType != typeof(THandler)))
            registration.Handlers.Add(new HandlerInvoker<TEvent, THandler>());  //aggiunge l'handler alla lista dell'evento, se non c'è gia
    }

    public bool TryGet(string eventName, [NotNullWhen(true)] out EventRegistration? registration) =>
        _registrations.TryGetValue(eventName, out registration);  //cerca la registrazione di un evento per nome. questo lo usa il consumer a ogni messaggio.
}

internal sealed class EventRegistration(string eventName, string exchange, Type eventType)
{
    public string EventName { get; } = eventName;
    public string Exchange { get; } = exchange;
    public Type EventType { get; } = eventType;
    public List<HandlerInvoker> Handlers { get; } = [];
}  //tutto cio che il consumer deve sapere su un evento!