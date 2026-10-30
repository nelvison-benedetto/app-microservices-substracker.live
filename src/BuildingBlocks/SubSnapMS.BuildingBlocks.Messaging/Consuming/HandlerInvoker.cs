using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Consuming;

/// <summary>
/// Chiama un handler tipizzato partendo da un IntegrationEvent generico, senza reflection a runtime:
/// i tipi sono fissati alla registrazione (AddConsumer&lt;TEvent, THandler&gt;).
/// </summary>
internal abstract class HandlerInvoker
{
    public abstract Type HandlerType { get; }

    /// <summary>Chiave dell'Inbox: stabile finché non rinomini o sposti la classe dell'handler.</summary>
    public string HandlerName => HandlerType.FullName ?? HandlerType.Name;

    public abstract Task InvokeAsync(IServiceProvider services, IntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);
}

internal sealed class HandlerInvoker<TEvent, THandler> : HandlerInvoker
    where TEvent : IntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    public override Type HandlerType => typeof(THandler);

    public override Task InvokeAsync(IServiceProvider services, IntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken) =>
        services.GetRequiredService<THandler>().HandleAsync((TEvent)integrationEvent, context, cancellationToken);
}