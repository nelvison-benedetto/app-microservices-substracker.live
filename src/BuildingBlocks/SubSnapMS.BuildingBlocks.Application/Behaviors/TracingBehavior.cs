using System.Diagnostics;
using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Uno span per ogni command/query (es. "LoginCommand"). Il nome della sorgente inizia con "SubSnapMS."
/// quindi Observability lo raccoglie in automatico. Sostituisce Logging, Performance e SentryTracing del monolite:
/// durata, errori e posizione nella trace distribuita sono tutti nello span.
/// </summary>
internal sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    internal static readonly ActivitySource ActivitySource = new("SubSnapMS.Application");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ActivitySource.StartActivity(typeof(TRequest).Name);
        activity?.SetTag("app.request.type", typeof(TRequest).FullName);

        try
        {
            return await next();
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddException(exception);
            throw;
        }
    }
}