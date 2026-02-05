using System;
using System.Security.Cryptography;

namespace MedStock.Services.Implementations
{
    internal static class PasswordHasher
    {
        // English: PBKDF2 parameters (must match how you store hashes)
        private const int Iterations = 100_000;
        private const int HashSizeBytes = 64; // matches VARBINARY(64)
        private const int SaltSizeBytes = 32; // matches VARBINARY(32)

        public static byte[] ComputeHash(string password, byte[] salt)
        {
            if (salt is null) throw new ArgumentNullException(nameof(salt));
            if (salt.Length != SaltSizeBytes) throw new ArgumentException($"Salt must be {SaltSizeBytes} bytes.");

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA512);
            return pbkdf2.GetBytes(HashSizeBytes);
        }

        public static bool Verify(string password, byte[] salt, byte[] expectedHash)
        {
            if (expectedHash is null) throw new ArgumentNullException(nameof(expectedHash));
            if (expectedHash.Length != HashSizeBytes) throw new ArgumentException($"Hash must be {HashSizeBytes} bytes.");

            var computed = ComputeHash(password, salt);
            return CryptographicOperations.FixedTimeEquals(computed, expectedHash);
        }
    }
}
