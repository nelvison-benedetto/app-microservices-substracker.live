using Microsoft.EntityFrameworkCore;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Serialization;
using SubSnapMS.Contracts.Abstractions;
using System.Diagnostics;
using System.Text.Json;

namespace SubSnapMS.BuildingBlocks.Messaging.Outbox;

//è il Transactional Outbox: un handler non può pubblicare su RabbitMQ e scrivere su Postgres ATOMICAMENTE. 

internal sealed class EfCoreOutbox<TContext>(TContext dbContext) : IIntegrationEventOutbox where TContext : DbContext
{

    public void Add<TEvent>(TEvent integrationEvent) where TEvent : IntegrationEvent  //generico con vincolo IntegrationEvent: si possono aggiungere solo integration event dei Contracts
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);  //un evento null, here lancia errore

        var eventType = integrationEvent.GetType();  //tipo reale dell'obj a runtime
        var metadata = EventMetadata.For(eventType);  //se l'event non ha un attributo, here lancia errore

        var current = Activity.Current;   //la trace della req che sta generando l'evento

        dbContext.Set<OutboxMessage>().Add(OutboxMessage.Create(
            integrationEvent.EventId,  //key della row, poi MessageId su rabbitmq, poi key dell' inbox del consumer.
            metadata.Name,      //routing key della pubblicazione
            metadata.Exchange,  //dove pubblicare
            JsonSerializer.Serialize(integrationEvent, eventType, MessagingJson.Options),  //corpo del mex
            integrationEvent.OccurredOnUtc,  //ordine di pubblicazione, timestamp del mex
            current?.Id,  //x continuare la trace alla pubblicazione
            current?.TraceStateString));  //dati aggiuntivi della trace
        //crea la riga e la aggiunge al DbContext

    }

}