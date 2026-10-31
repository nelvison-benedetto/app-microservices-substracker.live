using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace SubSnapMS.BuildingBlocks.Web.Responses;

/// <summary>
/// Risposte di successo per gli endpoint Minimal API, già nell'envelope.
/// Tipizzate (TypedResults): l'OpenAPI conosce lo schema esatto di ogni risposta.
/// Gli errori NON passano da qui: si lanciano eccezioni e li formatta ApiExceptionHandler.
/// </summary>
public static class ApiResponses
{
    public static Ok<ApiResult<T>> Ok<T>(T data) =>
        TypedResults.Ok(ApiResult<T>.Ok(data));

    public static Created<ApiResult<T>> Created<T>(string location, T data) =>
        TypedResults.Created(location, ApiResult<T>.Ok(data));

    /// <summary>204: operazione riuscita, nessun contenuto (es. logout, cambio email).</summary>
    public static NoContent NoContent() => TypedResults.NoContent();

    /// <summary>202: operazione accettata e completata in modo asincrono (es. cancellazione account).</summary>
    public static Accepted Accepted() => TypedResults.Accepted((string?)null);
}