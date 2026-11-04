using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using SubSnapMS.BuildingBlocks.Web.Responses;

namespace SubSnapMS.BuildingBlocks.Web.Errors;

//scrive un error nell'envelope ApiResult, con il TraceId della richiesta. chiunque nella libreria debba rispondere con un errore (le status code pages, l’exception handler) passa da qui 

public static class ApiErrorWriter
{
    public static Task WriteAsync(HttpContext context, ApiError error)  //riceve context cioè dove scrivere, e error cioè cosa scrivere
    {
        if (context.Response.HasStarted)  //se gli header delle risposta sono gia partiti verso il client, quindi ormai non si puo cambiare lo status code
            return Task.CompletedTask;  
        var errorWithTrace = error with  //with è l'operatore dei record, qui creiamo un clone e lo editiamo
        {
            TraceId = error.TraceId ?? Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier  //error.TraceId se il chiamante lo ha gia settato si mantiene quello, altrimenti il traceid Opentelemetry della req, altrimenti fallback finale l'id del Kestrel.
        };
        context.Response.StatusCode = error.StatusCode;
        context.Response.Headers.CacheControl = "no-store";  //browser, proxy e CDN non devono memoriazzare questo in cache
        return context.Response.WriteAsJsonAsync(ApiResult<object?>.Fail(errorWithTrace), context.RequestAborted);  //costruisce l’envelope standard della piattaforma, con success: false, data: null ed error valorizzato.  WriteAsJsonAsync() serializza l'obj in json con le tue opzioni di configurazione (ConfigureHttpJsonOptions.cs), imposta Content-Type: application/json; charset=utf-8 e scrive direttamente nello stream della risposta.
    }


}