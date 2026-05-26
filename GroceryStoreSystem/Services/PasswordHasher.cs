using System.Security.Cryptography;

namespace GroceryStoreSystem.Services;

public sealed class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 120_000;

    public (string Hash, string Salt) Create(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public bool Verify(string password, string storedHash, string storedSalt)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || string.IsNullOrWhiteSpace(storedSalt))
        {
            return false;
        }

        var salt = Convert.FromBase64String(storedSalt);
        var computed = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        var expected = Convert.FromBase64String(storedHash);
        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }
}
