using System.Security.Cryptography;
using System.Text;
using SchoolAdmission.Application.Abstractions;

namespace SchoolAdmission.Api.Auth;

/// <summary>
/// Stateless admin tokens: "payload.signature", where the payload is "admin.&lt;expiry unix seconds&gt;" and the
/// signature is HMAC-SHA256 of the payload. Stateless means every backend process accepts the same tokens.
/// </summary>
public sealed class AdminTokenService
{
    private readonly byte[] _key;
    private readonly TimeSpan _lifetime;
    private readonly IClock _clock;

    public AdminTokenService(AdminAuthOptions options, IClock clock)
    {
        _key = Encoding.UTF8.GetBytes(options.TokenSecret);
        _lifetime = options.TokenLifetime;
        _clock = clock;
    }

    public (string Token, DateTime ExpiresAt) Issue()
    {
        var expiresAt = _clock.UtcNow + _lifetime;
        var payload = $"admin.{new DateTimeOffset(expiresAt).ToUnixTimeSeconds()}";
        var encodedPayload = ToBase64Url(Encoding.UTF8.GetBytes(payload));
        return ($"{encodedPayload}.{Sign(encodedPayload)}", expiresAt);
    }

    public bool IsValid(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.');
        if (parts.Length != 2)
            return false;

        try
        {
            var expected = HMACSHA256.HashData(_key, Encoding.ASCII.GetBytes(parts[0]));
            if (!CryptographicOperations.FixedTimeEquals(expected, FromBase64Url(parts[1])))
                return false;

            var payload = Encoding.UTF8.GetString(FromBase64Url(parts[0])).Split('.');
            if (payload.Length != 2 || payload[0] != "admin" || !long.TryParse(payload[1], out var expiry))
                return false;

            return DateTimeOffset.FromUnixTimeSeconds(expiry) > new DateTimeOffset(_clock.UtcNow);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private string Sign(string encodedPayload) =>
        ToBase64Url(HMACSHA256.HashData(_key, Encoding.ASCII.GetBytes(encodedPayload)));

    private static string ToBase64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var s = value.Replace('-', '+').Replace('_', '/');
        s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
        return Convert.FromBase64String(s);
    }
}
