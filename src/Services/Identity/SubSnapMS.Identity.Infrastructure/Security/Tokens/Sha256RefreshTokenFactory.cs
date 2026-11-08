using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using SubSnapMS.Identity.Application.Abstractions.Security;

namespace SubSnapMS.Identity.Infrastructure.Security.Tokens;

//Refresh token: 64 byte casuali (512 bit) in base64url (~86 caratteri).
//Nel database solo SHA-256 in esadecimale: deterministico → lookup per uguaglianza con indice univoco.
//(l'old monolith SubSnap usava PasswordHasher: salt casuale → impossibile cercare per hash.) SHA-256 senza salt va bene qui: il token ha 512 bit di entropia, non è indovinabile come una password.
internal sealed class Sha256RefreshTokenFactory : IRefreshTokenFactory
{
    private const int TokenSizeInBytes = 64;

    public GeneratedRefreshToken Generate()
    {
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeInBytes));
        return new GeneratedRefreshToken(token, ComputeHash(token));
    }

    public string ComputeHash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}