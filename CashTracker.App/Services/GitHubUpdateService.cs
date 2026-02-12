using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.App.Services
{
    internal sealed class GitHubUpdateService
    {
        private readonly HttpClient _httpClient;

        public GitHubUpdateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UpdateCheckResult> CheckAsync(
            UpdateSettings settings,
            string currentVersionTag,
            CancellationToken ct = default)
        {
            if (settings is null || !settings.IsConfigured)
                return UpdateCheckResult.NotConfigured();

            var url = $"https://api.github.com/repos/{settings.RepoOwner}/{settings.RepoName}/releases/latest";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("CashTrackerApp/1.0");
            request.Headers.Accept.ParseAdd("application/vnd.github+json");

            using var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"GitHub release bilgisi alınamadı: {response.StatusCode} - {body}");

            using var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            var latestTag = ReadString(root, "tag_name");
            var releasePage = ReadString(root, "html_url");

            string assetName = string.Empty;
            string assetDownloadUrl = string.Empty;
            string checksumAssetName = string.Empty;
            string checksumAssetDownloadUrl = string.Empty;
            if (root.TryGetProperty("assets", out var assetsNode) && assetsNode.ValueKind == JsonValueKind.Array)
            {
                var assets = assetsNode.EnumerateArray().ToArray();
                var selectedAsset = SelectAsset(assets, settings.AssetName);
                if (selectedAsset.HasValue)
                {
                    assetName = ReadString(selectedAsset.Value, "name");
                    assetDownloadUrl = ReadString(selectedAsset.Value, "browser_download_url");

                    var selectedChecksum = SelectChecksumAsset(assets, assetName, settings.ChecksumAssetName);
                    if (selectedChecksum.HasValue)
                    {
                        checksumAssetName = ReadString(selectedChecksum.Value, "name");
                        checksumAssetDownloadUrl = ReadString(selectedChecksum.Value, "browser_download_url");
                    }
                }
            }

            var hasUpdate = !VersionEquals(currentVersionTag, latestTag);
            return new UpdateCheckResult(
                true,
                hasUpdate,
                latestTag,
                assetName,
                assetDownloadUrl,
                checksumAssetName,
                checksumAssetDownloadUrl,
                releasePage);
        }

        public async Task<string> DownloadAssetAsync(
            string assetUrl,
            string fileName,
            string appDataPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(assetUrl))
                throw new ArgumentException("Asset URL boş.", nameof(assetUrl));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Dosya adı boş.", nameof(fileName));

            if (string.IsNullOrWhiteSpace(appDataPath))
                throw new ArgumentException("Uygulama veri yolu boş.", nameof(appDataPath));

            var updatesDir = Path.Combine(appDataPath, "updates");
            Directory.CreateDirectory(updatesDir);

            var targetPath = Path.Combine(updatesDir, fileName);
            using var request = new HttpRequestMessage(HttpMethod.Get, assetUrl);
            request.Headers.UserAgent.ParseAdd("CashTrackerApp/1.0");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var file = File.Create(targetPath);
            await source.CopyToAsync(file, ct);

            return targetPath;
        }

        public async Task<string> DownloadTextAsync(
            string assetUrl,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(assetUrl))
                throw new ArgumentException("Asset URL boş.", nameof(assetUrl));

            using var request = new HttpRequestMessage(HttpMethod.Get, assetUrl);
            request.Headers.UserAgent.ParseAdd("CashTrackerApp/1.0");

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct);
        }

        private static JsonElement? SelectAsset(JsonElement[] assets, string preferredName)
        {
            if (assets.Length == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(preferredName))
            {
                var exact = assets.FirstOrDefault(a =>
                    string.Equals(ReadString(a, "name"), preferredName, StringComparison.OrdinalIgnoreCase));
                if (exact.ValueKind != JsonValueKind.Undefined)
                    return exact;
            }

            var preferredExtensions = new[] { ".msi", ".exe", ".zip" };
            foreach (var ext in preferredExtensions)
            {
                var candidate = assets.FirstOrDefault(a =>
                {
                    var name = ReadString(a, "name");
                    return name.EndsWith(ext, StringComparison.OrdinalIgnoreCase);
                });

                if (candidate.ValueKind != JsonValueKind.Undefined)
                    return candidate;
            }

            return assets[0];
        }

        private static JsonElement? SelectChecksumAsset(
            JsonElement[] assets,
            string selectedAssetName,
            string preferredChecksumName)
        {
            if (assets.Length == 0)
                return null;

            if (!string.IsNullOrWhiteSpace(preferredChecksumName))
            {
                var exact = assets.FirstOrDefault(a =>
                    string.Equals(ReadString(a, "name"), preferredChecksumName, StringComparison.OrdinalIgnoreCase));
                if (exact.ValueKind != JsonValueKind.Undefined)
                    return exact;
            }

            if (!string.IsNullOrWhiteSpace(selectedAssetName))
            {
                var sibling = assets.FirstOrDefault(a =>
                    string.Equals(
                        ReadString(a, "name"),
                        $"{selectedAssetName}.sha256",
                        StringComparison.OrdinalIgnoreCase));
                if (sibling.ValueKind != JsonValueKind.Undefined)
                    return sibling;
            }

            var fallback = assets.FirstOrDefault(a =>
                ReadString(a, "name").EndsWith(".sha256", StringComparison.OrdinalIgnoreCase));
            return fallback.ValueKind == JsonValueKind.Undefined ? null : fallback;
        }

        private static bool VersionEquals(string current, string latest)
        {
            var c = Normalize(current);
            var l = Normalize(latest);

            if (Version.TryParse(c, out var currentVersion) && Version.TryParse(l, out var latestVersion))
            {
                var cText = $"{currentVersion.Major}.{currentVersion.Minor}.{Math.Max(currentVersion.Build, 0)}";
                var lText = $"{latestVersion.Major}.{latestVersion.Minor}.{Math.Max(latestVersion.Build, 0)}";
                return string.Equals(cText, lText, StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(c, l, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var trimmed = value.Trim();
            return trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                ? trimmed[1..]
                : trimmed;
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var value))
                return string.Empty;

            return value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
        }
    }

    internal sealed record UpdateCheckResult(
        bool IsConfigured,
        bool HasUpdate,
        string LatestTag,
        string AssetName,
        string AssetDownloadUrl,
        string ChecksumAssetName,
        string ChecksumAssetDownloadUrl,
        string ReleasePageUrl)
    {
        public static UpdateCheckResult NotConfigured()
        {
            return new UpdateCheckResult(
                false,
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }
    }
}

