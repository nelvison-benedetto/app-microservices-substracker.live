using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.Contracts.Abstractions;

namespace SubSnapMS.BuildingBlocks.Messaging.Consuming;

//Esegue tutti gli handler di un evento. Per ognuno: scope DI nuovo, esecuzione "una volta sola" tramite l'Inbox (handler + record Inbox nella stessa transazione), retry con backoff esponenziale.
//Se un handler fallisce dopo tutti i retry, l'eccezione risale e il messaggio va in DLQ.
//Alla riconsegna (es. replay dalla DLQ) gli handler già riusciti vengono saltati dall'Inbox.


//MessageDispatcher è il componente che, quando arriva un messaggio, esegue tutti i suoi handler in modo affidabile. Per ogni handler mette insieme tre garanzie: uno scope DI nuovo a ogni tentativo, l’esecuzione una volta sola grazie all’Inbox, e i retry con attese crescenti. Se nonostante i retry un handler continua a fallire, lascia salire l’eccezione e il messaggio finisce nella DLQ.
internal sealed class MessageDispatcher(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<MessageDispatcher> logger)
{
    public async Task DispatchAsync(EventRegistration registration, IntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        foreach (var handler in registration.Handlers)
            await InvokeWithRetryAsync(handler, integrationEvent, context, cancellationToken);
    }

    private async Task InvokeWithRetryAsync(HandlerInvoker handler, IntegrationEvent integrationEvent, MessageContext context, CancellationToken cancellationToken)
    {
        var maxRetries = Math.Max(0, options.Value.MaxRetryAttempts);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                //Scope nuovo a ogni tentativo: DbContext pulito, nessuno stato residuo del tentativo fallito
                await using var scope = scopeFactory.CreateAsyncScope();  //await using chiude lo scope alla fine del tentativo
                var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>(); //List tipo dipende dal service (e.g. read models non ha nessun AddInbox quindi NullInboxStore, mentre e.g.Identity ha AddInbox<TContext>() quindi EfCoreInboxStore (on the same DbContext dell'handler))
                var attemptContext = context with { Attempt = attempt };

                var executed = await inbox.ExecuteOnceAsync(
                    context.MessageId,
                    handler.HandlerName,
                    token => handler.InvokeAsync(scope.ServiceProvider, integrationEvent, attemptContext, token),
                    cancellationToken);
                //si passa all'inbox key(id del mex e nome dell'handler), e cosa eseguire (lambda che chiama l’handler tramite il suo HandlerInvoker, con il provider di questo scope) 

                if (!executed)
                    logger.LogDebug("{Handler} already processed {MessageId}: skipped", handler.HandlerName, context.MessageId);

                return;
            }
            catch (Exception exception) when (attempt <= maxRetries && !cancellationToken.IsCancellationRequested)
            {
                var delay = options.Value.RetryBaseDelay * Math.Pow(2, attempt - 1);  //backoff esponenziale

                logger.LogWarning(exception,
                    "{Handler} failed on {EventName} {MessageId} (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}",
                    handler.HandlerName, context.EventName, context.MessageId, attempt, maxRetries + 1, delay);

                await Task.Delay(delay, cancellationToken);  //attende prima del prossimo ciclo del for

            }
        }
    }
}