using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Abstractions;

/// <summary>
/// Accoda un evento nell'Outbox del DbContext corrente. NON salva: l'evento viene scritto
/// dal prossimo SaveChanges (il TransactionBehavior), nella stessa transazione dei dati.
/// </summary>
public interface IIntegrationEventOutbox
{
    void Add<TEvent>(TEvent integrationEvent) where TEvent : IntegrationEvent;
}