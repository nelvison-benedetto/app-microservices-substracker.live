namespace SubSnapMS.BuildingBlocks.Messaging.Configuration;

//Nomi della topologia RabbitMQ derivati dal nome del servizio

internal sealed record MessagingSettings(string ServiceName)
{
    public string QueueName => $"{ServiceName}.queue";  //Queue del servizio, legata agli exchange degli eventi a cui è iscritto.

    public string DeadLetterExchange => $"{ServiceName}.dlx";

    public string DeadLetterQueue => $"{ServiceName}.queue.dlq";
}