using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using CashTracker.App.Services;
using CashTracker.Core.Models;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class UpdateManifestServiceTests
    {
        [Fact]
        public async Task CheckAsync_ValidSignedManifest_ReturnsUpdate()
        {
            using var rsa = RSA.Create(2048);
            var manifest = new SignedUpdateManifest
            {
                LatestVersion = "v1.2.0",
                MinSupportedVersion = "1.1.0",
                PackageUrl = "https://example.test/CashTracker-Setup.exe",
                Sha256 = new string('a', 64),
                ReleaseNotes = "Fast startup",
                IsMandatory = false
            };
            manifest.Signature = SignManifest(manifest, rsa);

            var handler = new RecordingHttpMessageHandler((_, _) =>
                RecordingHttpMessageHandler.OkJson(JsonSerializer.Serialize(manifest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })));

            using var http = new HttpClient(handler);
            var service = new UpdateManifestService(http, rsa.ToXmlString(false));

            var result = await service.CheckAsync(
                new UpdateSettings { ManifestUrl = "https://example.test/update-manifest.json" },
                "1.1.0");

            Assert.True(result.IsConfigured);
            Assert.True(result.HasUpdate);
            Assert.False(result.IsMandatory);
            Assert.Equal("v1.2.0", result.LatestVersion);
            Assert.Equal("CashTracker-Setup.exe", result.PackageFileName);
        }

        [Fact]
        public async Task CheckAsync_MandatoryManifest_ReportsMandatory()
        {
            using var rsa = RSA.Create(2048);
            var manifest = new SignedUpdateManifest
            {
                LatestVersion = "v1.2.0",
                MinSupportedVersion = "1.1.5",
                PackageUrl = "https://example.test/CashTracker-Setup.exe",
                Sha256 = new string('b', 64),
                ReleaseNotes = "Critical update",
                IsMandatory = true
            };
            manifest.Signature = SignManifest(manifest, rsa);

            var handler = new RecordingHttpMessageHandler((_, _) =>
                RecordingHttpMessageHandler.OkJson(JsonSerializer.Serialize(manifest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })));

            using var http = new HttpClient(handler);
            var service = new UpdateManifestService(http, rsa.ToXmlString(false));

            var result = await service.CheckAsync(
                new UpdateSettings { ManifestUrl = "https://example.test/update-manifest.json" },
                "1.1.0");

            Assert.True(result.HasUpdate);
            Assert.True(result.IsMandatory);
        }

        [Fact]
        public async Task CheckAsync_WhenManifestMissing_FallsBackToGitHubRelease()
        {
            var handler = new RecordingHttpMessageHandler((request, _) =>
            {
                var url = request.RequestUri?.ToString() ?? string.Empty;
                if (url.Contains("update-manifest.json"))
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("missing")
                    };
                }

                if (url.Contains("/repos/owner/repo/releases/latest"))
                {
                    return RecordingHttpMessageHandler.OkJson("""
                    {
                      "tag_name": "v1.2.0",
                      "html_url": "https://example.test/release",
                      "body": "Portable package",
                      "assets": [
                        {
                          "name": "CashTracker.exe",
                          "browser_download_url": "https://example.test/CashTracker.exe"
                        },
                        {
                          "name": "CashTracker.exe.sha256",
                          "browser_download_url": "https://example.test/CashTracker.exe.sha256"
                        }
                      ]
                    }
                    """);
                }

                if (url.EndsWith(".sha256"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent($"{new string('c', 64)}  CashTracker.exe")
                    };
                }

                throw new HttpRequestException($"Unexpected request: {url}");
            });

            using var http = new HttpClient(handler);
            var service = new UpdateManifestService(http);

            var result = await service.CheckAsync(
                new UpdateSettings
                {
                    ManifestUrl = "https://example.test/update-manifest.json",
                    RepoOwner = "owner",
                    RepoName = "repo"
                },
                "1.1.0");

            Assert.True(result.IsConfigured);
            Assert.True(result.HasUpdate);
            Assert.False(result.IsMandatory);
            Assert.False(result.CanInstallInApp);
            Assert.Equal("v1.2.0", result.LatestVersion);
            Assert.Equal("CashTracker.exe", result.PackageFileName);
            Assert.Equal(new string('c', 64), result.Sha256);
            Assert.Equal("https://example.test/release", result.ReleasePageUrl);
        }

        private static string SignManifest(SignedUpdateManifest manifest, RSA rsa)
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

            var payloadBytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var signature = rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Base64Url.Encode(signature);
        }
    }
}
