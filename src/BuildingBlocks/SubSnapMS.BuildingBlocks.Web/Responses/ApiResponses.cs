using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SubSnapMS.BuildingBlocks.Web.Responses;

//risposte di successo (200,201,204,202) per gli endpoint Minimal API. 
//!gli errori NON passano da qui: si lanciano eccezioni e li formatta ApiExceptionHandler.

public static class ApiResponses
{
    public static Ok<ApiResult<T>> Ok<T>(T data) =>  // Ok<T>(T data) è il tuo intrata
        TypedResults.Ok(ApiResult<T>.Ok(data));  //ApiResult<T>.Ok(data) crea l'envelope di successo

    public static Created<ApiResult<T>> Created<T>(string location, T data) =>
        TypedResults.Created(location, ApiResult<T>.Ok(data));  //location è l'url della nuova risorsa

    public static NoContent NoContent() => TypedResults.NoContent();
    //operazione riuscita, nessun contenuto (es. logout, cambio email)

    public static Accepted Accepted() => TypedResults.Accepted((string?)null);
    //operazione accettata e completata in modo sync (e.g. cancellazione account)

}  //TypedResults tipizza, l'OpenAPI conosce lo schema esatto di ogni risposta

/*
 {
  "success": true,
  "data": {
    "mediaId": "0193…",
    "uploadUrl": "https://…r2.cloudflarestorage.com/…",
    "objectKey": "avatars/…/….webp",
    "expiresAt": "2026-11-04T16:41:00+00:00",
    "requiredHeaders": { "Content-Type": "image/webp" }
  }
}
 */