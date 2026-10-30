namespace SubSnapMS.ApiGateway.Errors;

/// <summary>Messaggi standard per gli errori generati dal gateway stesso.</summary>
internal static class GatewayErrorMessages
{
    public static string For(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad request.",
        StatusCodes.Status401Unauthorized => "Authentication required.",
        StatusCodes.Status403Forbidden => "Access denied.",
        StatusCodes.Status404NotFound => "Resource not found.",
        StatusCodes.Status405MethodNotAllowed => "Method not allowed.",
        StatusCodes.Status413PayloadTooLarge => "Request body too large.",
        StatusCodes.Status429TooManyRequests => "Too many requests. Please try again later.",
        StatusCodes.Status502BadGateway => "The service could not be reached.",
        StatusCodes.Status503ServiceUnavailable => "Service temporarily unavailable.",
        StatusCodes.Status504GatewayTimeout => "The service did not respond in time.",
        >= 500 => "An unexpected error occurred.",
        _ => "The request could not be processed."
    };
}