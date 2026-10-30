using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

/// <summary>Informazioni sul messaggio in elaborazione, passate a ogni handler.</summary>
/// <param name="MessageId">Id del messaggio (= EventId).</param>
/// <param name="EventName">Nome versionato, es. "subscription.created.v1".</param>
/// <param name="SourceService">Servizio che ha pubblicato l'evento.</param>
/// <param name="Redelivered">True se RabbitMQ lo sta riconsegnando (es. dopo un crash del consumer).</param>
/// <param name="Attempt">Tentativo corrente dell'handler (1 = primo).</param>
public sealed record MessageContext(
    Guid MessageId,
    string EventName,
    string? SourceService,
    bool Redelivered,
    int Attempt);