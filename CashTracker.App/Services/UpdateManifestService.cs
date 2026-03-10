using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.App.Services
{
    internal class SignedUpdateManifestPayload
    {
        public string LatestVersion { get; set; } = string.Empty;
        public string MinSupportedVersion { get; set; } = string.Empty;
        public string PackageUrl { get; set; } = string.Empty;
        public string Sha256 { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
    }

    internal sealed class SignedUpdateManifest : SignedUpdateManifestPayload
    {
        public string Signature { get; set; } = string.Empty;
    }

    internal sealed record ManifestUpdateCheckResult(
        bool IsConfigured,
        bool HasUpdate,
        bool IsMandatory,
        string LatestVersion,
        string MinSupportedVersion,
        string PackageUrl,
        string PackageFileName,
        string Sha256,
        string ReleaseNotes,
        string ManifestUrl,
        bool CanInstallInApp,
        string ReleasePageUrl)
    {
        public static ManifestUpdateCheckResult NotConfigured()
        {
            return new ManifestUpdateCheckResult(
                false,
                false,
                false,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                false,
                string.Empty);
        }
    }

    internal sealed class UpdateManifestService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly HttpClient _httpClient;
        private readonly string _publicKeyXml;

        public UpdateManifestService(HttpClient httpClient, string? publicKeyXml = null)
        {
            _httpClient = httpClient;
            _publicKeyXml = string.IsNullOrWhiteSpace(publicKeyXml)
                ? AppSigningKeys.GetUpdateManifestPublicKeyXml()
                : publicKeyXml.Trim();
        }

        public async Task<ManifestUpdateCheckResult> CheckAsync(
            UpdateSettings settings,
            string currentVersionTag,
            CancellationToken ct = default)
        {
            if (settings is null)
                return ManifestUpdateCheckResult.NotConfigured();

            var manifestUrl = settings.ResolveManifestUrl();
            if (string.IsNullOrWhiteSpace(manifestUrl))
                return ManifestUpdateCheckResult.NotConfigured();

            HttpResponseMessage response;
            string body;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, manifestUrl);
                request.Headers.UserAgent.ParseAdd("CashTrackerApp/2.0");
                response = await _httpClient.SendAsync(request, ct);
                body = await response.Content.ReadAsStringAsync(ct);
            }
            catch (HttpRequestException ex)
            {
                var transportFallback = await TryCheckGitHubReleaseFallbackAsync(settings, currentVersionTag, ex.Message, ct);
                if (transportFallback is not null)
                    return transportFallback;

                throw new InvalidOperationException($"Update manifest alinamadi: {ex.Message}", ex);
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var fallback = await TryCheckGitHubReleaseFallbackAsync(
                        settings,
                        currentVersionTag,
                        $"{(int)response.StatusCode} {response.StatusCode}",
                        ct);
                    if (fallback is not null)
                        return fallback;

                    throw new InvalidOperationException($"Update manifest alinamadi: {response.StatusCode} - {body}");
                }

                var manifest = JsonSerializer.Deserialize<SignedUpdateManifest>(body, JsonOptions);
                if (manifest is null)
                    throw new InvalidOperationException("Update manifest okunamadi.");

                VerifyManifest(manifest, _publicKeyXml);

                var currentVersion = Normalize(currentVersionTag);
                var latestVersion = Normalize(manifest.LatestVersion);
                var minSupportedVersion = Normalize(manifest.MinSupportedVersion);
                var hasUpdate = HasNewerVersion(currentVersion, latestVersion);
                var isMandatory = manifest.IsMandatory && IsVersionLower(currentVersion, minSupportedVersion);

                return new ManifestUpdateCheckResult(
                    true,
                    hasUpdate || isMandatory,
                    isMandatory,
                    manifest.LatestVersion,
                    manifest.MinSupportedVersion,
                    manifest.PackageUrl,
                    GetFileNameFromUrl(manifest.PackageUrl),
                    manifest.Sha256,
                    manifest.ReleaseNotes,
                    manifestUrl,
                    IsInstallerPackage(manifest.PackageUrl),
                    ResolveReleasePageUrl(settings));
            }
        }

        public async Task<string> DownloadPackageAsync(
            string packageUrl,
            string packageFileName,
            string appDataPath,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(packageUrl))
                throw new ArgumentException("Paket adresi bos.", nameof(packageUrl));

            var updatesDir = Path.Combine(appDataPath, "updates");
            Directory.CreateDirectory(updatesDir);

            var targetPath = Path.Combine(updatesDir, packageFileName);
            using var request = new HttpRequestMessage(HttpMethod.Get, packageUrl);
            request.Headers.UserAgent.ParseAdd("CashTrackerApp/2.0");

            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            await using var file = File.Create(targetPath);
            await source.CopyToAsync(file, ct);
            return targetPath;
        }

        public static void VerifyPackageHash(string packagePath, string expectedSha256)
        {
            var actual = ComputeSha256(packagePath);
            var normalizedExpected = (expectedSha256 ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(normalizedExpected) ||
                !string.Equals(actual, normalizedExpected, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Indirilen paket dogrulanamadi.");
            }
        }

        internal static void VerifyManifest(SignedUpdateManifest manifest, string publicKeyXml)
        {
            var payload = new SignedUpdateManifestPayload
            {
                LatestVersion = manifest.LatestVersion,
                MinSupportedVersion = manifest.MinSupportedVersion,
                PackageUrl = manifest.PackageUrl,
                Sha256 = manifest.Sha256,
                ReleaseNotes = manifest.ReleaseNotes,
                IsMandatory = manifest.IsMandatory
            };

            var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);
            var signatureBytes = Base64Url.Decode(manifest.Signature);

            using var rsa = RSA.Create();
            rsa.FromXmlString(publicKeyXml);
            if (!rsa.VerifyData(payloadBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                throw new InvalidOperationException("Update manifest imzasi gecersiz.");
        }

        private static string ComputeSha256(string filePath)
        {
            using var stream = File.OpenRead(filePath);
            var hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool HasNewerVersion(string current, string latest)
        {
            if (string.Equals(current, latest, StringComparison.OrdinalIgnoreCase))
                return false;

            if (!TryParseComparableVersion(current, out var currentVersion))
                return !string.IsNullOrWhiteSpace(latest);

            if (!TryParseComparableVersion(latest, out var latestVersion))
                return false;

            return latestVersion > currentVersion;
        }

        private static bool IsVersionLower(string current, string minimum)
        {
            if (!TryParseComparableVersion(current, out var currentVersion))
                return false;

            if (!TryParseComparableVersion(minimum, out var minimumVersion))
                return false;

            return currentVersion < minimumVersion;
        }

        private static bool TryParseComparableVersion(string value, out Version version)
        {
            version = new Version(0, 0, 0, 0);
            if (string.IsNullOrWhiteSpace(value))
                return false;

            var digits = new string(value
                .Trim()
                .SkipWhile(ch => !char.IsDigit(ch))
                .TakeWhile(ch => char.IsDigit(ch) || ch == '.')
                .ToArray());

            var parts = digits
                .Split('.', StringSplitOptions.RemoveEmptyEntries)
                .Take(4)
                .Select(p => int.TryParse(p, out var n) ? n : -1)
                .ToArray();

            if (parts.Length == 0 || parts.Any(p => p < 0))
                return false;

            version = new Version(
                parts[0],
                parts.Length > 1 ? parts[1] : 0,
                parts.Length > 2 ? parts[2] : 0,
                parts.Length > 3 ? parts[3] : 0);
            return true;
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

        private async Task<ManifestUpdateCheckResult?> TryCheckGitHubReleaseFallbackAsync(
            UpdateSettings settings,
            string currentVersionTag,
            string manifestFailureReason,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(settings.RepoOwner) || string.IsNullOrWhiteSpace(settings.RepoName))
                return null;

            try
            {
                var githubService = new GitHubUpdateService(_httpClient);
                var release = await githubService.CheckAsync(settings, currentVersionTag, ct);
                var sha256 = await TryDownloadChecksumAsync(githubService, release.ChecksumAssetDownloadUrl, ct);

                return new ManifestUpdateCheckResult(
                    true,
                    release.HasUpdate,
                    false,
                    release.LatestTag,
                    string.Empty,
                    release.AssetDownloadUrl,
                    release.AssetName,
                    sha256,
                    release.ReleaseNotes,
                    settings.ResolveManifestUrl(),
                    IsInstallerPackage(release.AssetName),
                    release.ReleasePageUrl);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Update bilgisi alinamadi. Manifest: {manifestFailureReason}. GitHub: {ex.Message}",
                    ex);
            }
        }

        private static async Task<string> TryDownloadChecksumAsync(
            GitHubUpdateService githubService,
            string checksumUrl,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(checksumUrl))
                return string.Empty;

            try
            {
                var checksumText = await githubService.DownloadTextAsync(checksumUrl, ct);
                return ExtractSha256(checksumText);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ExtractSha256(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var match = Regex.Match(raw, "[a-fA-F0-9]{64}");
            return match.Success ? match.Value.ToLowerInvariant() : string.Empty;
        }

        private static bool IsInstallerPackage(string pathOrFileName)
        {
            if (string.IsNullOrWhiteSpace(pathOrFileName))
                return false;

            var name = Path.GetFileName(pathOrFileName).Trim();
            if (name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                return true;

            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                (name.Contains("setup", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("installer", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetFileNameFromUrl(string packageUrl)
        {
            if (string.IsNullOrWhiteSpace(packageUrl))
                return string.Empty;

            return Uri.TryCreate(packageUrl, UriKind.Absolute, out var uri)
                ? Path.GetFileName(uri.LocalPath)
                : Path.GetFileName(packageUrl);
        }

        private static string ResolveReleasePageUrl(UpdateSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.RepoOwner) || string.IsNullOrWhiteSpace(settings.RepoName))
                return string.Empty;

            return $"https://github.com/{settings.RepoOwner.Trim()}/{settings.RepoName.Trim()}/releases/latest";
        }
    }
}
