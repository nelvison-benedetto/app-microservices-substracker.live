using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SubSnapMS.Identity.Application.Abstractions.Security;

namespace SubSnapMS.Identity.Infrastructure.Security.Passwords;

/// <summary>
/// PasswordHasher di ASP.NET Core (formato V3: PBKDF2-HMAC-SHA512, salt casuale), lo stesso del monolite.
/// Iterazioni al valore raccomandato da OWASP: gli hash con meno iterazioni vengono verificati
/// e segnalati con SuccessRehashNeeded → ricalcolati automaticamente al login.
/// </summary>
internal sealed class AspNetPasswordHasher : IPasswordHasher
{
    private const int IterationCount = 210_000;

    // PasswordHasher<TUser> non usa l'utente: serve solo un oggetto qualsiasi
    private static readonly object Subject = new();

    private readonly PasswordHasher<object> _hasher = new(Options.Create(new PasswordHasherOptions
    {
        CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
        IterationCount = IterationCount
    }));

    // Hash fittizio con lo stesso costo di uno reale: usato quando l'email non esiste (timing attack)
    private readonly Lazy<string> _dummyHash;

    public AspNetPasswordHasher()
    {
        _dummyHash = new Lazy<string>(() => _hasher.HashPassword(Subject, Guid.NewGuid().ToString("N")));
    }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return _hasher.HashPassword(Subject, password);
    }

    public PasswordVerification Verify(string passwordHash, string password)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(password))
            return PasswordVerification.Failed;

        try
        {
            return _hasher.VerifyHashedPassword(Subject, passwordHash, password) switch
            {
                PasswordVerificationResult.Success => PasswordVerification.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
                _ => PasswordVerification.Failed
            };
        }
        catch (FormatException)
        {
            // Hash corrotto o in un formato sconosciuto: trattato come password errata, mai come errore 500
            return PasswordVerification.Failed;
        }
    }

    public void VerifyDummy(string password) =>
        _hasher.VerifyHashedPassword(Subject, _dummyHash.Value, password ?? string.Empty);
}