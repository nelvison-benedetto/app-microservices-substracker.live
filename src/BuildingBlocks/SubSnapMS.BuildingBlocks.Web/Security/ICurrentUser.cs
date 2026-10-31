namespace SubSnapMS.BuildingBlocks.Web.Security;

/// <summary>
/// Utente della richiesta corrente, ricavato SOLO dal JWT validato (mai da header come X-User-Id).
/// Ogni servizio converte UserId nel proprio Value Object.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>Lancia UnauthorizedException (401) se la richiesta non è autenticata.</summary>
    Guid UserId { get; }

    string? Email { get; }
}