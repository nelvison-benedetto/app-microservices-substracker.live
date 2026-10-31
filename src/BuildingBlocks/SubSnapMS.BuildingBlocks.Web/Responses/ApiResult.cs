namespace SubSnapMS.BuildingBlocks.Web.Responses;

/// <summary>
/// Envelope uniforme di ogni risposta di ogni servizio: { success, data, error }.
/// Stessa forma del monolite e dell'envelope del gateway.
/// </summary>
public sealed record ApiResult<T>(bool Success, T? Data, ApiError? Error)
{
    public static ApiResult<T> Ok(T data) => new(true, data, null);

    public static ApiResult<T> Fail(ApiError error) => new(false, default, error);
}