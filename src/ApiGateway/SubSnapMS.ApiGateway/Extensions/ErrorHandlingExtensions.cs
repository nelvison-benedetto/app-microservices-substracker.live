using SubSnapMS.ApiGateway.Errors;
using Yarp.ReverseProxy.Forwarder;

namespace SubSnapMS.ApiGateway.Extensions;

public static class ErrorHandlingExtensions
{
    /// <summary>
    /// 1. Eccezione non gestita → 500 con envelope (il middleware la logga; Sentry la cattura in automatico).
    /// 2. Risposta d'errore SENZA body (404 di route inesistente, 405, 502/503/504 di YARP, ecc.) → envelope.
    ///    Le risposte dei servizi che hanno già un body passano invariate.
    /// </summary>
    public static WebApplication UseGatewayErrorHandling(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp => errorApp.Run(context =>
            GatewayErrorWriter.WriteAsync(context, StatusCodes.Status500InternalServerError)));

        app.UseStatusCodePages(statusCodeContext =>
        {
            var context = statusCodeContext.HttpContext;
            return GatewayErrorWriter.WriteAsync(context, context.Response.StatusCode);
        });

        return app;
    }

    /// <summary>
    /// Errori di inoltro di YARP (servizio irraggiungibile, timeout, nessuna destinazione sana) → envelope ApiResult.
    /// Va registrato per primo nella pipeline del proxy, così vede l'esito finale dell'inoltro.
    /// </summary>
    public static IReverseProxyApplicationBuilder UseProxyErrorEnvelope(this IReverseProxyApplicationBuilder proxy)
    {
        proxy.Use(async (context, next) =>
        {
            await next();

            var error = context.Features.Get<IForwarderErrorFeature>();
            if (error is null || context.Response.HasStarted || context.RequestAborted.IsCancellationRequested)
                return;

            var statusCode = error.Error switch
            {
                ForwarderError.NoAvailableDestinations => StatusCodes.Status503ServiceUnavailable,
                ForwarderError.RequestTimedOut => StatusCodes.Status504GatewayTimeout,
                _ when context.Response.StatusCode >= 400 => context.Response.StatusCode,
                _ => StatusCodes.Status502BadGateway
            };

            await GatewayErrorWriter.WriteAsync(context, statusCode);
        });

        return proxy;
    }
}