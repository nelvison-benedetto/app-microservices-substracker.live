using System.Diagnostics;
using MediatR;

namespace SubSnapMS.BuildingBlocks.Application.Behaviors;

//Uno span per ogni command/query (es. "LoginCommand"). Il nome della sorgente inizia con "SubSnapMS." quindi Observability lo raccoglie in automatico. Sostituisce Logging, Performance e SentryTracing del monolite: durata, errori e posizione nella trace distribuita sono tutti nello span.

//TracingBehavior crea uno span OpenTelemetry per ogni command /query che passa da MediatR. quindi nella dashboard su opentelemetry vedi anche e.g. CreateSubscriptionCommand 88 ms” con dentro query sql / qualcosa che va storto / eccezzione.
//un behavior in MediatR è un middleware per i command e query di mediatr, RICORDI CHE NEL SubSnap hai i middleware a cipolla e TracingBehavior è il middleware piu esterno? questo here fa la stessa cosa.


internal sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    internal static readonly ActivitySource ActivitySource = new("SubSnapMS.Application");  //🔥ActivitySource è la sorgente da cui nascono gli span, .net uno span è un'Activity

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)  //metodo che mediatr chiama x ogni req, request(è il command o query), next(il resto della pipeline) e cancellationToken(x annullare se il cliente chiude browser)
    {
        using var activity = ActivitySource.StartActivity(typeof(TRequest).Name);  //apre lo span(che vedrai su dashboard opentelemetry) 
        activity?.SetTag("app.request.type", typeof(TRequest).FullName);  //aggiunge un attributo allo span, il nome completo del tipo con il namespace, e.g."SubSnapMS.Subscriptions.Application.Features.CreateSubscription.CreateSubscriptionCommand"
        try
        {
            return await next();  //esegue il resto della catena
        }
        catch (Exception exception)  //se qualcosa piu in basso lancia exception
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddException(exception);
            throw;
        }
    }

}


