namespace SubSnapMS.Contracts.Abstractions;

//base di ogni evento che attraversa i servizi via RabbitMQ.
//ogni evento concreto deve avere l'attributo [IntegrationEvent(exchange, name)].
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
    //identificativo univoco dell'evento (MessageId sul broker, chiave di deduplica nell'Inbox).

    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;
    //istante in cui il fatto è avvenuto (UTC), è solo un metadato tecnico 
}
