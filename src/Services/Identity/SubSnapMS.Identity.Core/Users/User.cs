using SubSnapMS.Identity.Core.RefreshTokens;

using SubSnapMS.Identity.Core.Users.Events;

/// <summary>
/// Rotazione: il token presentato viene revocato e sostituito da uno nuovo nella stessa famiglia.
/// Restituisce un risultato invece di lanciare eccezioni, perché la reuse detection modifica lo stato
/// e quella modifica DEVE essere salvata anche se la richiesta fallisce.
/// </summary>
/// <param name="reuseGracePeriod">
/// Un token ruotato da meno di questo intervallo viene ancora accettato: è un refresh parallelo
/// dello stesso client (es. due Server Actions insieme), non un furto. Dopo, il riuso revoca la famiglia.
/// </param>
public RefreshTokenRotationResult RotateRefreshToken(
    string presentedTokenHash,
    string newTokenHash,
    DateTimeOffset newTokenExpiresAtUtc,
    TimeSpan reuseGracePeriod,
    DateTimeOffset now)
{
    var current = _refreshTokens.FirstOrDefault(t => t.TokenHash == presentedTokenHash);
    if (current is null)
        return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.Invalid);

    if (!IsActive)
        return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.UserNotActive);

    if (current.IsExpired(now))
        return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.Expired);

    // Sessione chiusa (logout, cambio password, furto rilevato...): nessun token della famiglia è più valido
    if (IsFamilyClosed(current.FamilyId))
        return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.Invalid);

    var alreadyRotated = current.IsRevoked;   // qui il motivo è per forza Rotated

    if (alreadyRotated && now - current.RevokedAtUtc!.Value > reuseGracePeriod)
    {
        // Un token già ruotato viene riusato fuori tolleranza: è stato rubato. Si chiude tutta la sessione.
        RevokeFamily(current.FamilyId, RefreshTokenRevocationReason.ReuseDetected, now);
        Raise(new RefreshTokenReuseDetectedDomainEvent(Id, current.FamilyId, now));
        return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.ReuseDetected);
    }

    PruneExpiredRefreshTokens(now);

    var next = RefreshToken.Issue(newTokenHash, current.FamilyId, newTokenExpiresAtUtc, now);
    if (!alreadyRotated)
        current.Revoke(now, RefreshTokenRevocationReason.Rotated, replacedBy: next.Id);

    _refreshTokens.Add(next);
    return RefreshTokenRotationResult.Success(next);
}

/// <summary>Una famiglia è chiusa se almeno un suo token è stato revocato per un motivo diverso dalla rotazione.</summary>
private bool IsFamilyClosed(Guid familyId) =>
    _refreshTokens.Any(t =>
        t.FamilyId == familyId &&
        t.RevocationReason is not null &&
        t.RevocationReason != RefreshTokenRevocationReason.Rotated);