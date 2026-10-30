using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Consuming;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging;

public sealed class MessagingBuilder
{
    private readonly ConsumerRegistry _registry;

    internal MessagingBuilder(IServiceCollection services, ConsumerRegistry registry)
    {
        Services = services;
        _registry = registry;
    }

    public IServiceCollection Services { get; }

    /// <summary>Iscrive il servizio a un evento. Più handler per lo stesso evento sono ammessi.</summary>
    public MessagingBuilder AddConsumer<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        _registry.Add<TEvent, THandler>();
        Services.TryAddScoped<THandler>();
        return this;
    }

    /// <summary>
    /// Outbox transazionale sul DbContext del servizio: IIntegrationEventOutbox.Add() salva l'evento
    /// insieme ai dati, e OutboxProcessor lo pubblica su RabbitMQ.
    /// </summary>
    public MessagingBuilder AddOutbox<TContext>() where TContext : DbContext
    {
        Services.TryAddScoped<IIntegrationEventOutbox, EfCoreOutbox<TContext>>();
        Services.AddHostedService<OutboxProcessor<TContext>>();
        return this;
    }

    /// <summary>
    /// Inbox sul DbContext del servizio: ogni handler processa ogni messaggio una sola volta,
    /// e il record "processato" viene salvato nella stessa transazione delle modifiche dell'handler.
    /// </summary>
    public MessagingBuilder AddInbox<TContext>() where TContext : DbContext
    {
        Services.AddScoped<IInboxStore, EfCoreInboxStore<TContext>>();
        Services.AddHostedService<InboxCleanupService<TContext>>();
        return this;
    }
}