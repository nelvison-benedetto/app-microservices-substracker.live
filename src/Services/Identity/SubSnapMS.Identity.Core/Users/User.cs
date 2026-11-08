using SubSnapMS.BuildingBlocks.Domain;
using SubSnapMS.Identity.Core.RefreshTokens;
using SubSnapMS.Identity.Core.Users.Events;
using SubSnapMS.Identity.Core.Users.Exceptions;

namespace SubSnapMS.Identity.Core.Users;

//Aggregate root dell'identità. I RefreshToken sono figli: si creano, ruotano e revocano SOLO da qui.
//Il dominio non legge mai l'orologio: "now" arriva sempre come parametro (testabile).
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


    //Registrazione

    //Crea un nuovo utente attivo. L'unicità dell'email non è verificabile dal solo aggregate: la controlla l'Application (repository) e la garantisce l'indice univoco del database.
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

    //Sessioni (refresh token) 

    //Login riuscito (password già verificata dall'Application): apre una nuova sessione, cioè una nuova famiglia di refresh token.
    public RefreshToken SignIn(string refreshTokenHash, DateTimeOffset refreshTokenExpiresAtUtc, DateTimeOffset now)
    {
        EnsureActive();
        PruneExpiredRefreshTokens(now);

        var token = RefreshToken.Issue(refreshTokenHash, familyId: Guid.CreateVersion7(), refreshTokenExpiresAtUtc, now);
        _refreshTokens.Add(token);

        LastLoginAtUtc = now;
        return token;
    }

    //Rotazione: il token presentato viene revocato e sostituito da uno nuovo nella stessa famiglia.
    //Restituisce un risultato invece di lanciare eccezioni, perché la reuse detection modifica lo stato e quella modifica DEVE essere salvata anche se la richiesta fallisce.

    // <param name="reuseGracePeriod"> Un token ruotato da meno di questo intervallo viene ancora accettato: è un refresh parallelo dello stesso client (es. due Server Actions insieme), non un furto. Dopo, il riuso revoca la famiglia.
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

        //Sessione chiusa (logout, cambio password, furto rilevato...): nessun token della famiglia è più valido
        if (IsFamilyClosed(current.FamilyId))
            return RefreshTokenRotationResult.Failure(RefreshTokenRotationStatus.Invalid);

        var alreadyRotated = current.IsRevoked;   //qui il motivo è per forza Rotated

        if (alreadyRotated && now - current.RevokedAtUtc!.Value > reuseGracePeriod)
        {
            //Un token già ruotato viene riusato fuori tolleranza: è stato rubato. Si chiude tutta la sessione.
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

    //Logout: chiude la sessione (l'intera famiglia) del token indicato. False se non era attivo
    public bool SignOut(string refreshTokenHash, DateTimeOffset now)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.TokenHash == refreshTokenHash && !t.IsRevoked);
        if (token is null)
            return false;

        RevokeFamily(token.FamilyId, RefreshTokenRevocationReason.Logout, now);
        return true;
    }

    //Logout da tutti i dispositivi
    public void SignOutEverywhere(DateTimeOffset now) =>
        RevokeAllRefreshTokens(RefreshTokenRevocationReason.LogoutEverywhere, now);

    //Profilo 

    //L'unicità della nuova email è verificata dall'Application prima di chiamare questo metodo
    public void ChangeEmail(Email newEmail, DateTimeOffset now)
    {
        EnsureActive();

        if (newEmail == Email)
            return;

        Email = newEmail;
        UpdatedAtUtc = now;
        Raise(new UserEmailChangedDomainEvent(Id, newEmail, now));
    }

    //Nuova password (quella attuale è già stata verificata dall'Application).
    //Revoca TUTTE le sessioni: chi conosceva la vecchia password perde l'accesso.
    //L'Application apre poi una nuova sessione per il dispositivo che ha fatto il cambio.
    public void ChangePassword(PasswordHash newPasswordHash, DateTimeOffset now)
    {
        EnsureActive();

        PasswordHash = newPasswordHash;
        UpdatedAtUtc = now;
        RevokeAllRefreshTokens(RefreshTokenRevocationReason.PasswordChanged, now);
    }

    //Aggiorna l'hash dopo un login riuscito quando l'algoritmo di hashing è cambiato (stessa password, hash più robusto). Non revoca le sessioni.
    public void UpgradePasswordHash(PasswordHash rehashedPassword, DateTimeOffset now)
    {
        PasswordHash = rehashedPassword;
        UpdatedAtUtc = now;
    }


    //Cancellazione account

    //Avvia la saga: l'utente non può più autenticarsi, le sessioni vengono chiuse, gli altri servizi vengono avvisati (via domain event → integration event).
    //L'utente viene rimosso definitivamente solo quando tutti i servizi hanno confermato.
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

    //Regole interne 

    public void EnsureActive()
    {
        if (!IsActive)
            throw new UserNotActiveException();
    }

    //Una famiglia è chiusa se almeno un suo token è stato revocato per un motivo diverso dalla rotazione
    private bool IsFamilyClosed(Guid familyId) =>
        _refreshTokens.Any(t =>
            t.FamilyId == familyId &&
            t.RevocationReason is not null &&
            t.RevocationReason != RefreshTokenRevocationReason.Rotated);

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

    //Rimuove i token scaduti: non servono più nemmeno per la reuse detection (un token scaduto viene rifiutato comunque). Tiene la tabella piccola.
    private void PruneExpiredRefreshTokens(DateTimeOffset now) =>
        _refreshTokens.RemoveAll(t => t.IsExpired(now));
}