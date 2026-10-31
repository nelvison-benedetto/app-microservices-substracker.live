using SubSnapMS.BuildingBlocks.Domain;

namespace SubSnapMS.Identity.Core.RefreshTokens;

/// <summary>
/// Figlio dell'aggregate User: si crea e si revoca SOLO tramite User.
/// Contiene l'hash SHA-256 del token, mai il token in chiaro (che arriva solo al client).
/// FamilyId raggruppa tutti i token nati dallo stesso login (una "sessione").
/// </summary>
public sealed class RefreshToken : Entity<RefreshTokenId>
{
    public const int TokenHashMaxLength = 128;

    private RefreshToken() { }   // per EF Core

    public string TokenHash { get; private set; } = default!;
    public Guid FamilyId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public RefreshTokenRevocationReason? RevocationReason { get; private set; }
    public RefreshTokenId? ReplacedByTokenId { get; private set; }

    public bool IsRevoked => RevokedAtUtc.HasValue;

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;

    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    internal static RefreshToken Issue(string tokenHash, Guid familyId, DateTimeOffset expiresAtUtc, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (expiresAtUtc <= now)
            throw new ArgumentOutOfRangeException(nameof(expiresAtUtc), "A refresh token must expire in the future.");

        return new RefreshToken
        {
            Id = RefreshTokenId.New(),
            TokenHash = tokenHash,
            FamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc
        };
    }

    /// <summary>Idempotente: un token già revocato mantiene la prima revoca (motivo e data originali).</summary>
    internal void Revoke(DateTimeOffset now, RefreshTokenRevocationReason reason, RefreshTokenId? replacedBy = null)
    {
        if (IsRevoked)
            return;

        RevokedAtUtc = now;
        RevocationReason = reason;
        ReplacedByTokenId = replacedBy;
    }
}