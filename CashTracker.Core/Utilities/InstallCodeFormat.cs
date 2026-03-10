using System;

namespace CashTracker.Core.Utilities
{
    public static class InstallCodeFormat
    {
        public const string Prefix = "CTI";
        public const string Example = "CTI-1234ABCD-5678EF90-1357ACE0";

        public static bool TryNormalize(string? value, out string normalized)
        {
            normalized = Normalize(value);
            if (!IsValidNormalized(normalized))
            {
                normalized = string.Empty;
                return false;
            }

            return true;
        }

        public static string Normalize(string? value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool IsValidNormalized(string value)
        {
            if (value.Length != 30)
                return false;

            if (!value.StartsWith(Prefix + "-", StringComparison.Ordinal))
                return false;

            return IsHexBlock(value, 4) &&
                   value[12] == '-' &&
                   IsHexBlock(value, 13) &&
                   value[21] == '-' &&
                   IsHexBlock(value, 22);
        }

        private static bool IsHexBlock(string value, int startIndex)
        {
            for (var i = 0; i < 8; i++)
            {
                if (!IsHex(value[startIndex + i]))
                    return false;
            }

            return true;
        }

        private static bool IsHex(char value)
        {
            return (value >= '0' && value <= '9') ||
                   (value >= 'A' && value <= 'F');
        }
    }
}
