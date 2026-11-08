namespace SubSnapMS.Identity.Application.Common.Sessions;

//Coppia di token di una sessione. Il refresh token in chiaro esiste solo qui e nel client
public sealed record AuthTokensResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc)
{
    public override string ToString() =>
        $"AuthTokensResult {{ AccessTokenExpiresAtUtc = {AccessTokenExpiresAtUtc:O}, RefreshTokenExpiresAtUtc = {RefreshTokenExpiresAtUtc:O} }}";
}