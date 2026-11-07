using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Consuming;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.BuildingBlocks.Messaging.Outbox;
using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.BuildingBlocks.Messaging;

public sealed class MessagingBuilder
{
    private readonly ConsumerRegistry _registry;

    internal MessagingBuilder(IServiceCollection services, ConsumerRegistry registry)  //constructor
    {
        Services = services;
        _registry = registry;
    }

    public IServiceCollection Services { get; }


    //iscrive il servizio a un evento. Più handler per lo stesso evento sono ammessi
    public MessagingBuilder AddConsumer<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        _registry.Add<TEvent, THandler>();  //annota nel registro nome dell’evento, exchange, tipo e handler (il file che hai visto).
        Services.TryAddScoped<THandler>();  //registra l’handler nel DI, per la sua classe concreta. (TryAddScoped, cosi se 2 events usassero la stessa classe handler (classe che implementa 2 interfaces) la registrazione non viene duplicata)
        return this;
    }


    //🔥🔥“Questo servizio pubblica eventi tramite l’Outbox sul suo DbContext.” Registra le due metà del Transactional Outbox (cioe OutboxProcessor.cs e IIntegrationEventOutbox.cs)
    public MessagingBuilder AddOutbox<TContext>() where TContext : DbContext
    {
        Services.TryAddScoped<IIntegrationEventOutbox, EfCoreOutbox<TContext>>();
        Services.AddHostedService<OutboxProcessor<TContext>>();
        return this;
    }

    //“Questo servizio consuma eventi con garanzia di esecuzione unica, sul suo DbContext”
    public MessagingBuilder AddInbox<TContext>() where TContext : DbContext
    {
        Services.AddScoped<IInboxStore, EfCoreInboxStore<TContext>>();  //InboxStore ricorda che fa ogni handler gira in una transazione con il controllo e il record “già processato”
        Services.AddHostedService<InboxCleanupService<TContext>>();  //cleanup. ogni 6h cancella i records olds >30gg.
        return this;
    }



}