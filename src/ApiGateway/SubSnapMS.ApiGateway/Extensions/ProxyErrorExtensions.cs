using SubSnapMS.ApiGateway.Errors;

namespace SubSnapMS.ApiGateway.Extensions;

public static class ProxyErrorExtensions
{
    /// <summary>
    /// Quando YARP non riesce a inoltrare (servizio giù, timeout, nessuna destinazione sana)
    /// imposta solo lo status code, con body vuoto. Qui scriviamo un body nello stesso
    /// formato ApiResult che il frontend riceve già dai servizi.
    /// </summary>
    public static IApplicationBuilder UseProxyErrorEnvelope(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            await next();

            var error = context.GetForwarderErrorFeature();
            if (error is null || context.Response.HasStarted || context.RequestAborted.IsCancellationRequested)
                return;

            var statusCode = context.Response.StatusCode;
            var message = statusCode switch
            {
                StatusCodes.Status503ServiceUnavailable => "Service temporarily unavailable.",
                StatusCodes.Status504GatewayTimeout => "The service did not respond in time.",
                _ => "The service could not be reached."
            };

            await context.Response.WriteAsJsonAsync(GatewayErrorResponse.Create(statusCode, message));
        });
}
