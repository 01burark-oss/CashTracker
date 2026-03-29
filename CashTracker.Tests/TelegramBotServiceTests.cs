using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CashTracker.Infrastructure.Services;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class TelegramBotServiceTests
    {
        [Fact]
        public async Task GetUpdatesAsync_ParsesPhotoCaptionAndMessageId()
        {
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/getUpdates", StringComparison.OrdinalIgnoreCase))
                {
                    return RecordingHttpMessageHandler.OkJson(
                        "{\"ok\":true,\"result\":[{\"update_id\":77,\"message\":{\"message_id\":55,\"chat\":{\"id\":123},\"from\":{\"id\":42},\"caption\":\"market fis\",\"photo\":[{\"file_id\":\"small\",\"width\":90,\"height\":90,\"file_size\":200},{\"file_id\":\"large\",\"width\":800,\"height\":1200,\"file_size\":4000}]}}]}");
                }

                return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":[]}");
            });

            var bot = new TelegramBotService(new HttpClient(handler), "test-token");
            var updates = await bot.GetUpdatesAsync();

            var update = Assert.Single(updates);
            Assert.Equal(77, update.UpdateId);
            Assert.Equal(55, update.MessageId);
            Assert.Equal(123, update.ChatId);
            Assert.Equal(42, update.UserId);
            Assert.Equal("market fis", update.Caption);
            Assert.True(update.HasPhoto);
            Assert.Equal("large", update.PhotoFileId);
        }

        [Fact]
        public async Task GetFilePathAndDownloadFileAsync_UseTelegramFileEndpoints()
        {
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("/getFile", StringComparison.OrdinalIgnoreCase))
                {
                    return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{\"file_path\":\"photos/receipt.jpg\"}}");
                }

                if (url.Contains("/file/bottest-token/photos/receipt.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(new byte[] { 9, 8, 7 })
                    };
                }

                return RecordingHttpMessageHandler.OkJson("{\"ok\":true,\"result\":{}}");
            });

            var bot = new TelegramBotService(new HttpClient(handler), "test-token");
            var filePath = await bot.GetFilePathAsync("photo-1");
            Assert.Equal("photos/receipt.jpg", filePath);

            var tempPath = Path.Combine(Path.GetTempPath(), $"telegram_test_{Guid.NewGuid():N}.jpg");
            try
            {
                await bot.DownloadFileAsync(filePath, tempPath);
                Assert.True(File.Exists(tempPath));
                Assert.Equal(new byte[] { 9, 8, 7 }, await File.ReadAllBytesAsync(tempPath));
            }
            finally
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
        }
    }
}
