using System;
using System.Globalization;

namespace CashTracker.Core.Models
{
    public sealed class TelegramSettings
    {
        public string BotToken { get; set; } = "";
        public string ChatId { get; set; } = "";
        public bool EnableCommands { get; set; } = true;
        public string AllowedUserIds { get; set; } = "";
        public int PollTimeoutSeconds { get; set; } = 20;

        public bool IsEnabled =>
            !string.IsNullOrWhiteSpace(BotToken) &&
            !string.IsNullOrWhiteSpace(ChatId);

        public bool IsTargetChat(long chatId)
        {
            return string.Equals(
                ChatId,
                chatId.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal);
        }

        public bool IsAllowedUser(long? userId)
        {
            if (string.IsNullOrWhiteSpace(AllowedUserIds))
                return true;

            if (!userId.HasValue)
                return false;

            var entries = AllowedUserIds.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var raw in entries)
            {
                if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    continue;

                if (parsed == userId.Value)
                    return true;
            }

            return false;
        }

        public int GetSafePollTimeoutSeconds()
        {
            if (PollTimeoutSeconds < 1) return 1;
            if (PollTimeoutSeconds > 50) return 50;
            return PollTimeoutSeconds;
        }
    }
}
