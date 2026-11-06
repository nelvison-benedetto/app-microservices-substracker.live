namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

//Informazioni sul messaggio in elaborazione, passate a ogni handler.</summary>

public sealed record MessageContext(
    Guid MessageId,  //id del mex
    string EventName,  //nome versionato, e.g."subscription.created.v1"
    string? SourceService,  //service (ms) che ha pubblicato l'evento
    bool Redelivered,  //True se RabbitMQ lo sta riconsegnando (es. dopo un crash del consumer)
    int Attempt  //tentativo corrente dell'handler (1 = primo)
    );  