namespace SubSnapMS.BuildingBlocks.Domain;

//fatto avvenuto nel dominio, INTERNO al servizio (gestito in-process, nella stessa transazione).
//non va confuso con un IntegrationEvent di Contracts, che attraversa i servizi via RabbitMQ: un handler di domain event può tradurlo in integration event e metterlo nell'Outbox.
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }  //UTC timestamp

}