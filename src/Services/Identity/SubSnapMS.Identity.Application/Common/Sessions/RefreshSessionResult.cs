using SubSnapMS.Identity.Core.RefreshTokens;

namespace SubSnapMS.Identity.Application.Common.Sessions;

//Esito del refresh. Non è un'eccezione perché, in caso di reuse detection, la revoca della famiglia va salvata: il TransactionBehavior salva solo se l'handler termina senza eccezioni.
//L'endpoint trasforma un esito negativo in 401.
public sealed record RefreshSessionResult(RefreshTokenRotationStatus Status, AuthTokensResult? Tokens)
{
    public bool Succeeded => Status == RefreshTokenRotationStatus.Rotated && Tokens is not null;

    public static RefreshSessionResult Success(AuthTokensResult tokens) => new(RefreshTokenRotationStatus.Rotated, tokens);

    public static RefreshSessionResult Failure(RefreshTokenRotationStatus status) => new(status, null);
}