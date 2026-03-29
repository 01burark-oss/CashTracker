using CashTracker.Core.Models;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class TelegramSettingsTests
    {
        [Fact]
        public void IsAllowedUser_WithoutAllowedUsers_AllowsOnlyPrivateChatOwner()
        {
            var settings = new TelegramSettings
            {
                ChatId = "123456",
                AllowedUserIds = string.Empty
            };

            Assert.True(settings.IsAllowedUser(123456));
            Assert.False(settings.IsAllowedUser(42));
            Assert.False(settings.IsAllowedUser(null));
        }

        [Fact]
        public void IsAllowedUser_WithoutAllowedUsers_RejectsGroupChatByDefault()
        {
            var settings = new TelegramSettings
            {
                ChatId = "-100987654321",
                AllowedUserIds = string.Empty
            };

            Assert.False(settings.IsAllowedUser(42));
            Assert.False(settings.IsAllowedUser(null));
        }

        [Fact]
        public void IsAllowedUser_WithAllowedUsers_UsesConfiguredList()
        {
            var settings = new TelegramSettings
            {
                ChatId = "-100987654321",
                AllowedUserIds = "42,84"
            };

            Assert.True(settings.IsAllowedUser(42));
            Assert.True(settings.IsAllowedUser(84));
            Assert.False(settings.IsAllowedUser(7));
        }
    }
}
