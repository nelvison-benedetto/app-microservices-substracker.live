using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SubSnapMS.BuildingBlocks.Web.Responses;

namespace SubSnapMS.BuildingBlocks.Web.Errors;

/// <summary>Scrive un errore nell'envelope ApiResult, con il TraceId della richiesta.</summary>
public static class ApiErrorWriter
{
    public static Task WriteAsync(HttpContext context, ApiError error)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        var errorWithTrace = error with
        {
            TraceId = error.TraceId ?? Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier
        };

        context.Response.StatusCode = error.StatusCode;
        context.Response.Headers.CacheControl = "no-store";

        return context.Response.WriteAsJsonAsync(ApiResult<object?>.Fail(errorWithTrace), context.RequestAborted);
    }
}