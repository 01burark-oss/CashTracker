using System;
using System.Security.Cryptography;
using System.Text;

namespace CashTracker.Core.Utilities
{
    public static class InstallScopedSecretProtector
    {
        private const string Prefix = "ctsecret1:";
        private static readonly byte[] PurposeBytes = Encoding.UTF8.GetBytes("cashtracker-license-receipt-ocr");

        public static string Protect(string secret, string installCode)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return string.Empty;

            if (!InstallCodeFormat.TryNormalize(installCode, out var normalizedInstallCode))
                throw new ArgumentException("Install code is invalid.", nameof(installCode));

            var key = DeriveKey(normalizedInstallCode);
            var nonce = RandomNumberGenerator.GetBytes(12);
            var clearBytes = Encoding.UTF8.GetBytes(secret.Trim());
            var cipherBytes = new byte[clearBytes.Length];
            var tagBytes = new byte[16];

            using var aes = new AesGcm(key, tagBytes.Length);
            aes.Encrypt(nonce, clearBytes, cipherBytes, tagBytes, PurposeBytes);
            return $"{Prefix}{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(cipherBytes)}.{Convert.ToBase64String(tagBytes)}";
        }

        public static bool TryUnprotect(string protectedSecret, string installCode, out string secret)
        {
            secret = string.Empty;
            if (string.IsNullOrWhiteSpace(protectedSecret))
                return false;

            if (!InstallCodeFormat.TryNormalize(installCode, out var normalizedInstallCode))
                return false;

            var raw = protectedSecret.Trim();
            if (!raw.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            var parts = raw[Prefix.Length..].Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                return false;

            try
            {
                var nonce = Convert.FromBase64String(parts[0]);
                var cipherBytes = Convert.FromBase64String(parts[1]);
                var tagBytes = Convert.FromBase64String(parts[2]);

                var clearBytes = new byte[cipherBytes.Length];
                using var aes = new AesGcm(DeriveKey(normalizedInstallCode), tagBytes.Length);
                aes.Decrypt(nonce, cipherBytes, tagBytes, clearBytes, PurposeBytes);
                secret = Encoding.UTF8.GetString(clearBytes).Trim();
                return !string.IsNullOrWhiteSpace(secret);
            }
            catch
            {
                secret = string.Empty;
                return false;
            }
        }

        private static byte[] DeriveKey(string normalizedInstallCode)
        {
            var input = Encoding.UTF8.GetBytes($"cashtracker-license::{normalizedInstallCode}");
            return SHA256.HashData(input);
        }
    }
}
