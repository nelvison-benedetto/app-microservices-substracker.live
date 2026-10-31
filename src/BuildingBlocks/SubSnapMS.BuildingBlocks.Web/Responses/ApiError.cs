using System.Text.Json.Serialization;

namespace SubSnapMS.BuildingBlocks.Web.Responses;

/// <summary>
/// Dettaglio dell'errore. StatusCode e Message ci sono sempre (compatibili con il frontend attuale);
/// Code, Errors e TraceId sono opzionali e vengono omessi dal JSON quando vuoti.
/// </summary>
/// <param name="Code">Identificativo stabile (es. "user.email_taken"): il frontend può tradurlo con next-intl.</param>
/// <param name="Errors">Errori di validazione per campo (nomi in camelCase).</param>
/// <param name="TraceId">Id della trace distribuita (= header X-Correlation-Id).</param>
public sealed record ApiError(
    int StatusCode,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Code = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, string[]>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceId = null);