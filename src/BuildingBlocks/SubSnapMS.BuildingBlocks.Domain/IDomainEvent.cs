using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Domain;

/// <summary>
/// Fatto avvenuto nel dominio, INTERNO al servizio (gestito in-process, nella stessa transazione).
/// Non va confuso con un IntegrationEvent di Contracts, che attraversa i servizi via RabbitMQ:
/// un handler di domain event può tradurlo in integration event e metterlo nell'Outbox.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}