using System;

namespace CashTracker.App.Services
{
    internal static class Base64Url
    {
        public static string Encode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static byte[] Decode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Array.Empty<byte>();

            var normalized = value
                .Trim()
                .Replace('-', '+')
                .Replace('_', '/');

            while ((normalized.Length % 4) != 0)
                normalized += "=";

            return Convert.FromBase64String(normalized);
        }
    }
}
