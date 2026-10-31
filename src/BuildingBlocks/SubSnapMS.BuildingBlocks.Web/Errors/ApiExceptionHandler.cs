using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.BuildingBlocks.Web.Responses;

namespace SubSnapMS.BuildingBlocks.Web.Errors;

/// <summary>
/// Traduce ogni eccezione nell'envelope ApiResult con lo status code giusto.
/// Errori attesi (4xx): log Information, nessuno stack trace. Errori inattesi (5xx): log Error → Sentry.
/// Il messaggio di un 500 è sempre generico: i dettagli interni non arrivano mai al client.
/// </summary>
internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Il client ha chiuso la connessione: non c'è nessuno a cui rispondere
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            logger.LogDebug("Request {Method} {Path} cancelled by the client", httpContext.Request.Method, httpContext.Request.Path);
            return true;
        }

        var error = Map(exception);

        if (error.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("{Method} {Path} → {StatusCode} {ErrorCode}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, error.StatusCode, error.Code, error.Message);
        }

        await ApiErrorWriter.WriteAsync(httpContext, error);
        return true;
    }

    private static ApiError Map(Exception exception) => exception switch
    {
        ValidationException validation => FromValidation(validation),

        // Dalla categoria più specifica alla più generica (sono tutte DomainException)
        NotFoundException e => new ApiError(StatusCodes.Status404NotFound, e.Message, e.Code),
        ConflictException e => new ApiError(StatusCodes.Status409Conflict, e.Message, e.Code),
        UnauthorizedException e => new ApiError(StatusCodes.Status401Unauthorized, e.Message, e.Code),
        ForbiddenException e => new ApiError(StatusCodes.Status403Forbidden, e.Message, e.Code),
        DomainException e => new ApiError(StatusCodes.Status400BadRequest, e.Message, e.Code),

        // JSON malformato, body troppo grande, parametri non leggibili
        BadHttpRequestException e => new ApiError(e.StatusCode, ApiErrorMessages.For(e.StatusCode), ApiErrorMessages.CodeFor(e.StatusCode)),

        _ => new ApiError(StatusCodes.Status500InternalServerError, ApiErrorMessages.For(500), ApiErrorMessages.CodeFor(500))
    };

    private static ApiError FromValidation(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => ToCamelCase(failure.PropertyName))
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        // Il primo messaggio come messaggio principale: il frontend attuale mostra error.message
        var message = exception.Errors.FirstOrDefault()?.ErrorMessage ?? "One or more validation errors occurred.";

        return new ApiError(StatusCodes.Status400BadRequest, message, "validation_failed", errors);
    }

    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName) ? "request" : JsonNamingPolicy.CamelCase.ConvertName(propertyName);
}