using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
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

                var messageId = 0;
                if (messageNode.TryGetProperty("message_id", out var messageIdNode) &&
                    messageIdNode.TryGetInt32(out var parsedMessageId))
                {
                    messageId = parsedMessageId;
                }

                if (!TryReadNestedInt64(messageNode, "chat", "id", out var chatId))
                    continue;

                var text = ReadOptionalString(messageNode, "text");
                var caption = ReadOptionalString(messageNode, "caption");
                var photoFileId = TryReadLargestPhotoFileId(messageNode, out var resolvedFileId)
                    ? resolvedFileId
                    : string.Empty;

                long? userId = null;
                if (TryReadNestedInt64(messageNode, "from", "id", out var fromId))
                    userId = fromId;

                if (string.IsNullOrWhiteSpace(text) &&
                    string.IsNullOrWhiteSpace(caption) &&
                    string.IsNullOrWhiteSpace(photoFileId))
                {
                    continue;
                }

                updates.Add(new TelegramUpdate
                {
                    UpdateId = updateId,
                    MessageId = messageId,
                    ChatId = chatId,
                    UserId = userId,
                    Text = text ?? string.Empty,
                    Caption = caption ?? string.Empty,
                    PhotoFileId = photoFileId
                });
            }

            return updates;
        }

        public async Task<string> GetFilePathAsync(string fileId, CancellationToken ct = default)
        {
            EnsureConfigured();
            if (string.IsNullOrWhiteSpace(fileId))
                throw new ArgumentException("FileId is required.", nameof(fileId));

            var url = $"{_baseUrl}/getFile?file_id={WebUtility.UrlEncode(fileId)}";
            using var response = await _http.GetAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Telegram getFile failed: {response.StatusCode} - {body}");

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("ok", out var okNode) ||
                okNode.ValueKind != JsonValueKind.True)
            {
                throw new InvalidOperationException($"Telegram getFile returned ok=false: {body}");
            }

            if (!doc.RootElement.TryGetProperty("result", out var resultNode) ||
                resultNode.ValueKind != JsonValueKind.Object ||
                !resultNode.TryGetProperty("file_path", out var pathNode) ||
                pathNode.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("Telegram file_path not found.");
            }

            var filePath = pathNode.GetString();
            if (string.IsNullOrWhiteSpace(filePath))
                throw new InvalidOperationException("Telegram file_path is empty.");

            return filePath.Trim();
        }

        public async Task DownloadFileAsync(
            string filePath,
            string destinationPath,
            CancellationToken ct = default)
        {
            EnsureConfigured();
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("File path is required.", nameof(filePath));
            if (string.IsNullOrWhiteSpace(destinationPath))
                throw new ArgumentException("Destination path is required.", nameof(destinationPath));

            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            var normalizedPath = filePath.TrimStart('/');
            var url = $"https://api.telegram.org/file/{_baseUrl[( _baseUrl.IndexOf("bot", StringComparison.Ordinal) )..]}/{normalizedPath}";
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Telegram file download failed: {response.StatusCode} - {body}");
            }

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var target = File.Create(destinationPath);
            await source.CopyToAsync(target, ct);
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

        private static string? ReadOptionalString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out var node) || node.ValueKind != JsonValueKind.String)
                return null;

            return node.GetString();
        }

        private static bool TryReadLargestPhotoFileId(JsonElement messageNode, out string fileId)
        {
            fileId = string.Empty;

            if (!messageNode.TryGetProperty("photo", out var photoNode) || photoNode.ValueKind != JsonValueKind.Array)
                return false;

            string? bestFileId = null;
            long bestSize = -1;

            foreach (var photo in photoNode.EnumerateArray())
            {
                if (!photo.TryGetProperty("file_id", out var fileIdNode) || fileIdNode.ValueKind != JsonValueKind.String)
                    continue;

                var candidateFileId = fileIdNode.GetString();
                if (string.IsNullOrWhiteSpace(candidateFileId))
                    continue;

                long size = 0;
                if (photo.TryGetProperty("file_size", out var fileSizeNode) && fileSizeNode.TryGetInt64(out var fileSize))
                    size = fileSize;

                if (photo.TryGetProperty("width", out var widthNode) && widthNode.TryGetInt32(out var width) &&
                    photo.TryGetProperty("height", out var heightNode) && heightNode.TryGetInt32(out var height))
                {
                    size = Math.Max(size, (long)width * height);
                }

                if (size <= bestSize)
                    continue;

                bestSize = size;
                bestFileId = candidateFileId.Trim();
            }

            if (string.IsNullOrWhiteSpace(bestFileId))
                return false;

            fileId = bestFileId;
            return true;
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
        public int MessageId { get; set; }
        public long ChatId { get; set; }
        public long? UserId { get; set; }
        public string Text { get; set; } = "";
        public string Caption { get; set; } = "";
        public string PhotoFileId { get; set; } = "";
        public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoFileId);
    }
}
