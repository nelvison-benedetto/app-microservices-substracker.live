using Microsoft.Extensions.DependencyInjection;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.BuildingBlocks.Messaging.Consuming;

//HandlerInvoker è il pezzo che esegue l’handler giusto quando arriva un messaggio. Il consumer ha in mano un IntegrationEvent “generico”, appena deserializzato, e deve chiamare un handler tipizzato come UserRegisteredHandler.HandleAsync(UserRegistered …); quindi questo file fa da ponte.

internal abstract class HandlerInvoker  //NON GENERICA
{
    public abstract Type HandlerType { get; }  //tipo della classe handler (typeof(UserRegisteredHandler)). Lo usa ConsumerRegistry.Add per evitare di registrare due volte lo stesso handler e.g. if (registration.Handlers.All(handler => handler.HandlerType != typeof(THandler))){...}

    public string HandlerName => HandlerType.FullName ?? HandlerType.Name;  //key dell'Inbox e.g. "SubSnapMS.Subscriptions.Application.IntegrationEventHandlers.UserDeletionRequestedHandler" , lo usa l'Inbox che dice “il messaggio X è stato processato dall’handler Y”

    public abstract Task InvokeAsync(IServiceProvider services, IntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken);  //metodo che esegue l'handler, accetta l’evento come IntegrationEvent generico: il chiamante (il dispatcher) non deve conoscere il tipo concreto
}

//ora fai tutto realmente (questa è classe concreta)
internal sealed class HandlerInvoker<TEvent, THandler> : HandlerInvoker
    where TEvent : IntegrationEvent
    where THandler : class, IIntegrationEventHandler<TEvent>
{
    public override Type HandlerType => typeof(THandler);

    public override Task InvokeAsync(IServiceProvider services, IntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken) =>
        services.GetRequiredService<THandler>().HandleAsync((TEvent)integrationEvent, context, cancellationToken);
}

