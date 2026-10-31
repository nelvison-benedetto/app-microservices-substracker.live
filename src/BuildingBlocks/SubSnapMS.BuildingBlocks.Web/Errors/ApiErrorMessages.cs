using Microsoft.AspNetCore.Http;

namespace SubSnapMS.BuildingBlocks.Web.Errors;

/// <summary>Messaggi e codici standard per gli errori senza un'eccezione specifica (status code pages, 500).</summary>
internal static class ApiErrorMessages
{
    public static string For(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad request.",
        StatusCodes.Status401Unauthorized => "Authentication required.",
        StatusCodes.Status403Forbidden => "Access denied.",
        StatusCodes.Status404NotFound => "Resource not found.",
        StatusCodes.Status405MethodNotAllowed => "Method not allowed.",
        StatusCodes.Status409Conflict => "The request conflicts with the current state.",
        StatusCodes.Status413PayloadTooLarge => "Request body too large.",
        StatusCodes.Status415UnsupportedMediaType => "Unsupported media type.",
        StatusCodes.Status429TooManyRequests => "Too many requests. Please try again later.",
        StatusCodes.Status503ServiceUnavailable => "Service temporarily unavailable.",
        >= 500 => "An unexpected error occurred.",
        _ => "The request could not be processed."
    };

    public static string CodeFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "bad_request",
        StatusCodes.Status401Unauthorized => "unauthorized",
        StatusCodes.Status403Forbidden => "forbidden",
        StatusCodes.Status404NotFound => "not_found",
        StatusCodes.Status405MethodNotAllowed => "method_not_allowed",
        StatusCodes.Status409Conflict => "conflict",
        StatusCodes.Status413PayloadTooLarge => "payload_too_large",
        StatusCodes.Status415UnsupportedMediaType => "unsupported_media_type",
        StatusCodes.Status429TooManyRequests => "too_many_requests",
        StatusCodes.Status503ServiceUnavailable => "service_unavailable",
        >= 500 => "internal_error",
        _ => "error"
    };
}