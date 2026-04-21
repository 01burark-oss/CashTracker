using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using CashTracker.Core.Services;

namespace CashTracker.Infrastructure.Services
{
    [SupportedOSPlatform("windows")]
    public sealed class DpapiSecretProtector : ISecretProtector
    {
        private const string Prefix = "dpapi1:";

        public string Protect(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                return string.Empty;

            var clearBytes = Encoding.UTF8.GetBytes(secret.Trim());
            var cipherBytes = ProtectedData.Protect(clearBytes, null, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(cipherBytes);
        }

        public bool TryUnprotect(string protectedSecret, out string secret)
        {
            secret = string.Empty;
            if (string.IsNullOrWhiteSpace(protectedSecret))
                return false;

            var raw = protectedSecret.Trim();
            if (!raw.StartsWith(Prefix, StringComparison.Ordinal))
                return false;

            try
            {
                var cipherBytes = Convert.FromBase64String(raw[Prefix.Length..]);
                var clearBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                secret = Encoding.UTF8.GetString(clearBytes).Trim();
                return !string.IsNullOrWhiteSpace(secret);
            }
            catch
            {
                secret = string.Empty;
                return false;
            }
        }
    }
}
