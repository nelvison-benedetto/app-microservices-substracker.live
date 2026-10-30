namespace SubSnapMS.ApiGateway.Errors;


/// <summary>
/// Stessa forma di ApiResult dei servizi: { success, data, error: { statusCode, message } }.
/// Il frontend gestisce gli errori del gateway come quelli di qualsiasi servizio.
/// </summary>
public sealed record GatewayErrorResponse(bool Success, object? Data, GatewayError Error)
{
    public static GatewayErrorResponse Create(int statusCode, string message) =>
        new(false, null, new GatewayError(statusCode, message));
}

public sealed record GatewayError(int StatusCode, string Message);