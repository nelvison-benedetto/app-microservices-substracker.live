using SubSnapMS.ApiGateway.Errors;

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
}