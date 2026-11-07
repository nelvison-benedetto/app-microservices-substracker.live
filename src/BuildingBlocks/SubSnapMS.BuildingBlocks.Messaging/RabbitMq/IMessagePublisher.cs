namespace SubSnapMS.BuildingBlocks.Messaging.RabbitMq;

//publish di basso livello, usata dall'OutboxProcessor
internal interface IMessagePublisher
{
    Task PublishRawAsync(OutgoingMessage message, CancellationToken cancellationToken = default);  //Raw means che riceve il mex gia serializzato (in bytes), con Task ovviamente il metodo termina solo quando rabbitmq ha la publish confims.
}

