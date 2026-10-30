using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SubSnapMS.BuildingBlocks.Messaging.Abstractions;
using SubSnapMS.BuildingBlocks.Messaging.Configuration;
using SubSnapMS.BuildingBlocks.Messaging.Inbox;
using SubSnapMS.Contracts.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SubSnapMS.BuildingBlocks.Messaging.Consuming;

/// <summary>
/// Esegue tutti gli handler di un evento. Per ognuno: scope DI nuovo, controllo Inbox, handler,
/// registrazione nell'Inbox (stessa transazione), retry con backoff esponenziale.
/// Se un handler fallisce dopo tutti i retry, l'eccezione risale e il messaggio va in DLQ.
/// Grazie all'Inbox, alla riconsegna gli handler già riusciti vengono saltati.
/// </summary>
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
                await using var scope = scopeFactory.CreateAsyncScope();
                var inbox = scope.ServiceProvider.GetRequiredService<IInboxStore>();

                if (await inbox.HasBeenProcessedAsync(context.MessageId, handler.HandlerName, cancellationToken))
                {
                    logger.LogDebug("{Handler} already processed {MessageId}: skipped", handler.HandlerName, context.MessageId);
                    return;
                }

                await handler.InvokeAsync(scope.ServiceProvider, integrationEvent, context with { Attempt = attempt }, cancellationToken);

                // Con l'Inbox EF: salva le modifiche dell'handler + il record Inbox in un unico SaveChanges
                await inbox.MarkProcessedAsync(context.MessageId, handler.HandlerName, cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt <= maxRetries && !cancellationToken.IsCancellationRequested)
            {
                var delay = options.Value.RetryBaseDelay * Math.Pow(2, attempt - 1);

                logger.LogWarning(exception,
                    "{Handler} failed on {EventName} {MessageId} (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}",
                    handler.HandlerName, context.EventName, context.MessageId, attempt, maxRetries + 1, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}