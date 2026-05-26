using System.Security.Cryptography;
using System.Text;

namespace GroceryStoreSystem.Services;

public sealed class MobileAccessTokenService(IConfiguration configuration)
{
    private readonly string _secret = configuration["MobileAccess:Secret"] ?? "ChangeThisSecretBeforeProduction";
    private readonly int _tokenDays = int.TryParse(configuration["MobileAccess:TokenDays"], out var days) ? days : 30;

    public string CreateToken()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_tokenDays).ToUnixTimeSeconds();
        var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(expiresAt.ToString()));
        var signature = Sign(payload);
        return $"{payload}.{signature}";
    }

    public bool Validate(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split('.', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var expected = Sign(parts[0]);
        if (!FixedEquals(expected, parts[1]))
        {
            return false;
        }

        var payloadText = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
        return long.TryParse(payloadText, out var expiresAt)
            && DateTimeOffset.UtcNow.ToUnixTimeSeconds() <= expiresAt;
    }

    private string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_secret));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static bool FixedEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}
