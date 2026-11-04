namespace SubSnapMS.BuildingBlocks.Web.Responses;

//Envelope uniforme di ogni risposta di ogni servizio: { success, data, error }, questa struttura è anche stata utilizzata nel tuo monolith SubSnap.

public sealed record ApiResult<T>(bool Success, T? Data, ApiError? Error)
{
    public static ApiResult<T> Ok(T data) => new(true, data, null);

    public static ApiResult<T> Fail(ApiError error) => new(false, default, error);
}