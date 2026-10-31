using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.BuildingBlocks.Application.DomainEvents;

/// <summary>
/// Risolve gli handler dallo scope corrente (stessi repository e stesso DbContext della richiesta).
/// Gli handler vengono risolti al momento del dispatch, non nel costruttore: così non si crea
/// una dipendenza circolare DbContext → dispatcher → handler → repository → DbContext.
/// Il tipo generico dell'invoker è creato una volta per tipo di evento e poi riusato (nessuna reflection a ogni evento).
/// </summary>
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

    private abstract class HandlerInvoker
    {
        public abstract Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider services, CancellationToken cancellationToken);
    }

    private sealed class HandlerInvoker<TDomainEvent> : HandlerInvoker
        where TDomainEvent : IDomainEvent
    {
        public override async Task InvokeAsync(IDomainEvent domainEvent, IServiceProvider services, CancellationToken cancellationToken)
        {
            // In sequenza: condividono lo stesso DbContext, che non è thread-safe
            foreach (var handler in services.GetServices<IDomainEventHandler<TDomainEvent>>())
                await handler.HandleAsync((TDomainEvent)domainEvent, cancellationToken);
        }
    }
}