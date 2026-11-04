using System.Text.Json.Serialization;

namespace SubSnapMS.BuildingBlocks.Web.Responses;

//dettaglio dell'errore. StatusCode e Message ci sono sempre (compatibili con il frontend attuale); Code, Errors e TraceId sono opzionali e vengono omessi dal JSON quando vuoti.

public sealed record ApiError(  //ur struttura di un error
    int StatusCode,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Code = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, string[]>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceId = null);

//[property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] dice a System.Text.Json di ignorare questo field il campo dal JSON quando vale null 

/*
 {
  "success": false,
  "data": null,
  "error": {                              ← questo è un ApiError
    "statusCode": 404,
    "message": "Subscription not found.",
    "code": "subscription.not_found",
    "traceId": "4bf92f3577b34da6a3ce929d0e0e4736"
  }
}
 */