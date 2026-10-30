namespace SubSnapMS.ApiGateway.Errors;

internal static class GatewayErrorWriter
{
    /// <summary>Scrive status code + envelope JSON. Da usare solo se la risposta non è ancora iniziata.</summary>
    public static Task WriteAsync(HttpContext context, int statusCode, string? message = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.Headers.CacheControl = "no-store";

        return context.Response.WriteAsJsonAsync(
            GatewayErrorResponse.Create(statusCode, message ?? GatewayErrorMessages.For(statusCode)),
            context.RequestAborted);
    }
}