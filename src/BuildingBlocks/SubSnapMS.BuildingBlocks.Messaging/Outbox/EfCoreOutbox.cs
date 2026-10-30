using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Messaging.Outbox;

internal sealed class EfCoreOutbox<TContext>(TContext dbContext) : IIntegrationEventOutbox
    where TContext : DbContext
{
    public void Add<TEvent>(TEvent integrationEvent) where TEvent : IntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventType = integrationEvent.GetType();
        var metadata = EventMetadata.For(eventType);
        var current = Activity.Current;   // la trace della richiesta che sta generando l'evento

        dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
            integrationEvent.EventId,
            metadata.Name,
            metadata.Exchange,
            JsonSerializer.Serialize(integrationEvent, eventType, MessagingJson.Options),
            integrationEvent.OccurredOnUtc,
            current?.Id,
            current?.TraceStateString));
    }
}