using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using CashTracker.App.Services;
using CashTracker.Core.Models;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class PinReminderServiceTests
    {
        [Fact]
        public async Task SendCurrentPinAsync_WhenTelegramConfigured_SendsPinToTelegram()
        {
            var security = new FakeAppSecurityService();
            await security.SetPinAsync("2468");

            var handler = new RecordingHttpMessageHandler();
            var http = new HttpClient(handler);
            var settings = new TelegramSettings
            {
                BotToken = "test-token",
                ChatId = "123"
            };

            var backup = new BackupReportService(
                new TelegramBotService(http, settings.BotToken),
                settings,
                new FakeDailyReportService(),
                new DatabaseBackupService(new DatabasePaths(Path.Combine(
                    Path.GetTempPath(),
                    $"cashtracker_tests_{Guid.NewGuid():N}.db"))));

            var service = new PinReminderService(security, backup, settings);

            var result = await service.SendCurrentPinAsync();

            Assert.Equal(PinReminderStatus.Success, result.Status);
            var text = handler.GetLastFormFieldValue("/sendMessage", "text");
            Assert.Contains("PIN: 2468", text);
        }

        [Fact]
        public async Task SendCurrentPinAsync_WhenTelegramNotConfigured_ReturnsNotConfigured()
        {
            var security = new FakeAppSecurityService();
            var handler = new RecordingHttpMessageHandler();
            var http = new HttpClient(handler);
            var settings = new TelegramSettings();

            var backup = new BackupReportService(
                new TelegramBotService(http, settings.BotToken),
                settings,
                new FakeDailyReportService(),
                new DatabaseBackupService(new DatabasePaths(Path.Combine(
                    Path.GetTempPath(),
                    $"cashtracker_tests_{Guid.NewGuid():N}.db"))));

            var service = new PinReminderService(security, backup, settings);

            var result = await service.SendCurrentPinAsync();

            Assert.Equal(PinReminderStatus.NotConfigured, result.Status);
            Assert.Null(handler.GetLastFormFieldValue("/sendMessage", "text"));
        }
    }
}
