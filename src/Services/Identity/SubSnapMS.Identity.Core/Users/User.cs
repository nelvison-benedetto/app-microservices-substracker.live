using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.BuildingBlocks.Domain.Exceptions;
using SubSnapMS.Identity.Core.RefreshTokens;
using SubSnapMS.Identity.Core.Users.Events;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Core.Users;

/// <summary>
/// Aggregate root dell'identità. I RefreshToken sono figli: si creano, ruotano e revocano SOLO da qui.
/// Il dominio non legge mai l'orologio: "now" arriva sempre come parametro (testabile).
/// </summary>
public sealed class User : AggregateRoot<UserId>
{
    private readonly List<RefreshToken> _refreshTokens = [];

    private User() { }   // per EF Core

    public Email Email { get; private set; } = default!;
    public PasswordHash PasswordHash { get; private set; } = default!;
    public UserStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public DateTimeOffset? DeletionRequestedAtUtc { get; private set; }

    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    public bool IsActive => Status == UserStatus.Active;

    // ── Registrazione ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un nuovo utente attivo. L'unicità dell'email non è verificabile dal solo aggregate:
    /// la controlla l'Application (repository) e la garantisce l'indice univoco del database.
    /// </summary>
    public static User Register(Email email, PasswordHash passwordHash, DateTimeOffset now)
    {
        var user = new User
        {
            Id = UserId.New(),
            Email = email,
            PasswordHash = passwordHash,
            Status = UserStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id, email, now));
        return user;
    }

    // ── Sessioni (refresh token) ──────────────────────────────────────────────────

    /// <summary>
    /// Login riuscito (password già verificata dall'Application): apre una nuova sessione,
    /// cioè una nuova famiglia di refresh token.
    /// </summary>
    public RefreshToken SignIn(string refreshTokenHash, DateTimeOffset refreshTokenExpiresAtUtc, DateTimeOffset now)
    {
        EnsureActive();
        PruneExpiredRefreshTokens(now);

        var token = RefreshToken.Issue(refreshTokenHash, familyId: Guid.CreateVersion7(), refreshTokenExpiresAtUtc, now);
        _refreshTokens.Add(token);

        LastLoginAtUtc = now;
        return token;
    }

    /// <summary>
    /// Rotazione: il token presentato viene revocato e sostituito da uno nuovo nella stessa famiglia.
    /// Restituisce un risultato invece di lanciare eccezioni, perché alcuni esiti (reuse detection)
    /// modificano lo stato e quella modifica DEVE essere salvata anche se la richiesta fallisce.
    /// </summary>
    /// <param name="reuseGracePeriod">
    /// Un token ruotato da meno di questo intervallo è considerato un refresh concorrente legittimo
    /// (due richieste parallele dello stesso client), non un furto.
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

        if (current.IsRevoked)
        {
            if (current.RevocationReason != RefreshTokenRevocationReason.Rotated)
                return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.Invalid);

            if (now - current.RevokedAtUtc!.Value <= reuseGracePeriod)
                return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.ConcurrentRefresh);

            // Un token già ruotato viene riusato: è stato rubato. Si chiude tutta la sessione.
            RevokeFamily(current.FamilyId, RefreshTokenRevocationReason.ReuseDetected, now);
            Raise(new RefreshTokenReuseDetectedDomainEvent(Id, current.FamilyId, now));
            return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.ReuseDetected);
        }

        if (current.IsExpired(now))
            return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.Expired);

        PruneExpiredRefreshTokens(now);

        var next = RefreshToken.Issue(newTokenHash, current.FamilyId, newTokenExpiresAtUtc, now);
        current.Revoke(now, RefreshTokenRevocationReason.Rotated, replacedBy: next.Id);
        _refreshTokens.Add(next);

        return RefreshTokenRotationResult.Success(next);
    }

    /// <summary>Logout: chiude la sessione (l'intera famiglia) del token indicato. False se non era attivo.</summary>
    public bool SignOut(string refreshTokenHash, DateTimeOffset now)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == refreshTokenHash && !t.IsRevoked);
        if (token is null)
            return false;

        RevokeFamily(token.FamilyId, RefreshTokenRevocationReason.Logout, now);
        return true;
    }

    /// <summary>Logout da tutti i dispositivi.</summary>
    public void SignOutEverywhere(DateTimeOffset now) =>
        RevokeAllRefreshTokens(RefreshTokenRevocationReason.LogoutEverywhere, now);

    // ── Profilo ───────────────────────────────────────────────────────────────────

    /// <summary>L'unicità della nuova email è verificata dall'Application prima di chiamare questo metodo.</summary>
    public void ChangeEmail(Email newEmail, DateTimeOffset now)
    {
        EnsureActive();

        if (newEmail == Email)
            return;

        Email = newEmail;
        UpdatedAtUtc = now;
        Raise(new UserEmailChangedDomainEvent(Id, newEmail, now));
    }

    /// <summary>
    /// Nuova password (quella attuale è già stata verificata dall'Application).
    /// Revoca TUTTE le sessioni: chi conosceva la vecchia password perde l'accesso.
    /// L'Application apre poi una nuova sessione per il dispositivo che ha fatto il cambio.
    /// </summary>
    public void ChangePassword(PasswordHash newPasswordHash, DateTimeOffset now)
    {
        EnsureActive();

        PasswordHash = newPasswordHash;
        UpdatedAtUtc = now;
        RevokeAllRefreshTokens(RefreshTokenRevocationReason.PasswordChanged, now);
    }

    /// <summary>
    /// Aggiorna l'hash dopo un login riuscito quando l'algoritmo di hashing è cambiato
    /// (stessa password, hash più robusto). Non revoca le sessioni.
    /// </summary>
    public void UpgradePasswordHash(PasswordHash rehashedPassword, DateTimeOffset now)
    {
        PasswordHash = rehashedPassword;
        UpdatedAtUtc = now;
    }

    // ── Cancellazione account ─────────────────────────────────────────────────────

    /// <summary>
    /// Avvia la saga: l'utente non può più autenticarsi, le sessioni vengono chiuse,
    /// gli altri servizi vengono avvisati (via domain event → integration event).
    /// L'utente viene rimosso definitivamente solo quando tutti i servizi hanno confermato.
    /// </summary>
    public void RequestDeletion(DateTimeOffset now)
    {
        if (Status == UserStatus.PendingDeletion)
            return;

        Status = UserStatus.PendingDeletion;
        DeletionRequestedAtUtc = now;
        UpdatedAtUtc = now;
        RevokeAllRefreshTokens(RefreshTokenRevocationReason.AccountDeletion, now);

        Raise(new UserDeletionRequestedDomainEvent(Id, now));
    }

    // ── Regole interne ────────────────────────────────────────────────────────────

    public void EnsureActive()
    {
        if (!IsActive)
            throw new UserNotActiveException();
    }

    private void RevokeFamily(Guid familyId, RefreshTokenRevocationReason reason, DateTimeOffset now)
    {
        foreach (var token in _refreshTokens.Where(t => t.FamilyId == familyId))
            token.Revoke(now, reason);
    }

    private void RevokeAllRefreshTokens(RefreshTokenRevocationReason reason, DateTimeOffset now)
    {
        foreach (var token in _refreshTokens)
            token.Revoke(now, reason);
    }

    /// <summary>
    /// Rimuove i token scaduti: non servono più nemmeno per la reuse detection
    /// (un token scaduto viene rifiutato comunque). Tiene la tabella piccola.
    /// </summary>
    private void PruneExpiredRefreshTokens(DateTimeOffset now) =>
        _refreshTokens.RemoveAll(t => t.IsExpired(now));
}