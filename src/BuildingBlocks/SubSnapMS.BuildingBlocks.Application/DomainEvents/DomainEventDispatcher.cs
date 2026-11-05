using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.BuildingBlocks.Application.DomainEvents;

//dato un domain event, trova ed esegue i suoi handlers.
//E' chiamato da ServiceDbContext durante il salvataggio, una volta per ogni event.
//come tecnica usi il 'invoker generico', gli handler vengono risolti al momento del dispatch, non nel costruttore: così non si crea una dipendenza circolare DbContext → dispatcher → handler → repository → DbContext.
//Il tipo generico dell'invoker è creato una volta per tipo di evento e poi riusato (nessuna reflection a ogni evento).

internal sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, HandlerInvoker> Invokers = new();

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        var invoker = Invokers.GetOrAdd(domainEvent.GetType(), static eventType =>
            (HandlerInvoker)Activator.CreateInstance(typeof(HandlerInvoker<>).MakeGenericType(eventType))!);

        return invoker.InvokeAsync(domainEvent, serviceProvider, cancellationToken);
    }

    private abstract class HandlerInvoker   //astratta NON GENERICA, accetta un IDomainEvent qualsiasi, essendo non generica tutti gli invoker possono stare in una sola collezzione dello stesso tipo.
    {
        public abstract Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TDomainEvent> : HandlerInvoker
        where TDomainEvent : IDomainEvent   //GENERICA! impelmenta l'abstract HandlerInvoker, e dentro TDomainEvent è un tipo noto, quindi si puo scrivere codice generico nomrale verificato dal compiler! 
    {
        public override async Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider services, CancellationToken cancellationToken)
        {
            //In sequenza: condividono lo stesso DbContext, che non è thread-safe
            foreach (var handler in services.GetServices<IDomainEventHandler<TDomainEvent>>())
                await handler.HandleAsync((TDomainEvent)domainEvent, cancellationToken);
        }
    }


}