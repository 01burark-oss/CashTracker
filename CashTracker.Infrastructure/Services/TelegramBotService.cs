using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CashTracker.Infrastructure.Services
{
    public sealed class TelegramBotService
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public TelegramBotService(HttpClient httpClient, string botToken)
        {
            _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            if (string.IsNullOrWhiteSpace(botToken))
            {
                _baseUrl = string.Empty;
                return;
            }

            _baseUrl = $"https://api.telegram.org/bot{botToken}";
        }

        public async Task SendTextAsync(string chatId, string text, CancellationToken ct = default)
        {
            EnsureConfigured();
            if (string.IsNullOrWhiteSpace(chatId)) throw new ArgumentException("ChatId is required.", nameof(chatId));
            if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Text is required.", nameof(text));

            var url = $"{_baseUrl}/sendMessage";
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["chat_id"] = chatId,
                ["text"] = text
            });

            using var response = await _http.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Telegram sendMessage failed: {response.StatusCode} - {body}");
        }

        public async Task SendDocumentAsync(string chatId, string filePath, string? caption = null, CancellationToken ct = default)
        {
            EnsureConfigured();
            if (string.IsNullOrWhiteSpace(chatId)) throw new ArgumentException("ChatId is required.", nameof(chatId));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path is required.", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found.", filePath);

            var url = $"{_baseUrl}/sendDocument";

            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(chatId), "chat_id");

            if (!string.IsNullOrWhiteSpace(caption))
                form.Add(new StringContent(caption), "caption");

            var fileName = Path.GetFileName(filePath);
            using var fileStream = await OpenReadableFileWithRetryAsync(filePath, ct);
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            form.Add(fileContent, "document", fileName);

            using var response = await _http.PostAsync(url, form, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Telegram sendDocument failed: {response.StatusCode} - {body}");
        }

        private static async Task<FileStream> OpenReadableFileWithRetryAsync(string filePath, CancellationToken ct)
        {
            const int maxAttempts = 6;
            Exception? lastError = null;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return new FileStream(
                        filePath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);
                }
                catch (IOException ex) when (attempt < maxAttempts)
                {
                    lastError = ex;
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct);
                }
                catch (UnauthorizedAccessException ex) when (attempt < maxAttempts)
                {
                    lastError = ex;
                    await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), ct);
                }
            }

            throw new InvalidOperationException("Document could not be opened for upload.", lastError);
        }

        public async Task<IReadOnlyList<TelegramUpdate>> GetUpdatesAsync(
            long? offset = null,
            int timeoutSeconds = 20,
            CancellationToken ct = default)
        {
            EnsureConfigured();

            var timeout = Math.Clamp(timeoutSeconds, 0, 50);
            var url = $"{_baseUrl}/getUpdates?timeout={timeout}";
            if (offset.HasValue)
                url += $"&offset={offset.Value}";

            using var response = await _http.GetAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Telegram getUpdates failed: {response.StatusCode} - {body}");

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("ok", out var okNode) &&
                okNode.ValueKind == JsonValueKind.False)
            {
                throw new InvalidOperationException($"Telegram getUpdates returned ok=false: {body}");
            }

            if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
                return Array.Empty<TelegramUpdate>();

            var updates = new List<TelegramUpdate>();

            foreach (var item in result.EnumerateArray())
            {
                if (!item.TryGetProperty("update_id", out var updateIdNode) || !updateIdNode.TryGetInt64(out var updateId))
                    continue;

                if (!item.TryGetProperty("message", out var messageNode) || messageNode.ValueKind != JsonValueKind.Object)
                    continue;

                if (!TryReadNestedInt64(messageNode, "chat", "id", out var chatId))
                    continue;

                if (!messageNode.TryGetProperty("text", out var textNode) || textNode.ValueKind != JsonValueKind.String)
                    continue;

                var text = textNode.GetString();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                long? userId = null;
                if (TryReadNestedInt64(messageNode, "from", "id", out var fromId))
                    userId = fromId;

                updates.Add(new TelegramUpdate
                {
                    UpdateId = updateId,
                    ChatId = chatId,
                    UserId = userId,
                    Text = text
                });
            }

            return updates;
        }

        private static bool TryReadNestedInt64(JsonElement root, string objectName, string propertyName, out long value)
        {
            value = 0;

            if (!root.TryGetProperty(objectName, out var objectNode) || objectNode.ValueKind != JsonValueKind.Object)
                return false;

            if (!objectNode.TryGetProperty(propertyName, out var propertyNode))
                return false;

            return propertyNode.TryGetInt64(out value);
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
                throw new InvalidOperationException("Telegram bot token is missing. Set Telegram:BotToken in appsettings.json.");
        }
    }

    public sealed class TelegramUpdate
    {
        public long UpdateId { get; set; }
        public long ChatId { get; set; }
        public long? UserId { get; set; }
        public string Text { get; set; } = "";
    }
}
