using System.Net.Http;
using System.Threading.Tasks;
using CashTracker.App.Services;
using CashTracker.Core.Models;
using CashTracker.Tests.Support;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class GitHubUpdateServiceTests
    {
        [Theory]
        [InlineData("1.0.3", "1.0.3")]
        [InlineData("1.0.3", "v1.0.3")]
        [InlineData("1.0.3.0", "v1.0.3")]
        [InlineData("v1.0.3", "1.0.3")]
        public async Task CheckAsync_SameVersionFormats_DoesNotReportUpdate(string currentVersion, string latestVersion)
        {
            var handler = new RecordingHttpMessageHandler((_, _) =>
                RecordingHttpMessageHandler.OkJson($$"""
                {
                  "tag_name": "{{latestVersion}}",
                  "html_url": "https://example.test/release"
                }
                """));
            using var http = new HttpClient(handler);
            var service = new GitHubUpdateService(http);

            var result = await service.CheckAsync(
                new UpdateSettings { RepoOwner = "owner", RepoName = "repo" },
                currentVersion);

            Assert.True(result.IsConfigured);
            Assert.False(result.HasUpdate);
            Assert.Contains("/repos/owner/repo/releases/latest", handler.Requests[0].Url);
        }

        [Fact]
        public async Task CheckAsync_NewerPatchVersion_ReportsUpdate()
        {
            var handler = new RecordingHttpMessageHandler((_, _) =>
                RecordingHttpMessageHandler.OkJson("""
                {
                  "tag_name": "v1.0.4",
                  "html_url": "https://example.test/release"
                }
                """));
            using var http = new HttpClient(handler);
            var service = new GitHubUpdateService(http);

            var result = await service.CheckAsync(
                new UpdateSettings { RepoOwner = "owner", RepoName = "repo" },
                "1.0.3");

            Assert.True(result.HasUpdate);
            Assert.Equal("v1.0.4", result.LatestTag);
        }
    }
}
