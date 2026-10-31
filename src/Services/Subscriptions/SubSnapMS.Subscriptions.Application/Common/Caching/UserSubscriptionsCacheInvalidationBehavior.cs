using MediatR;
using Microsoft.Extensions.Logging;

namespace SubSnapMS.Subscriptions.Application.Common.Caching;

/// <summary>
/// Il più esterno della pipeline (registrato prima dei behaviors comuni): quando next() ritorna,
/// la transazione è già stata salvata. Invalida la cache solo se il command è riuscito.
/// Un errore della cache non fa fallire la richiesta: i dati sono salvati, la cache scade comunque.
/// </summary>
internal sealed class UserSubscriptionsCacheInvalidationBehavior<TRequest, TResponse>(
    UserSubscriptionsCache cache,
    ILogger<UserSubscriptionsCacheInvalidationBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IInvalidatesUserSubscriptions invalidating)
        {
            try
            {
                await cache.InvalidateAsync(invalidating.UserId, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogWarning(exception, "Could not invalidate the subscriptions cache of user {UserId}", invalidating.UserId);
            }
        }

        return response;
    }
}