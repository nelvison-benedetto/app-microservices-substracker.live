namespace SubSnapMS.Contracts.Abstractions;

/// <summary>
/// Base di ogni evento che attraversa i servizi via RabbitMQ.
/// Ogni evento concreto deve avere l'attributo [IntegrationEvent(exchange, name)].
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Identificativo univoco dell'evento (MessageId sul broker, chiave di deduplica nell'Inbox).</summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    /// <summary>Istante in cui il fatto è avvenuto (UTC).</summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
}
