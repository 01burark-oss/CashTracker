using System.IO;
using CashTracker.App.Services;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class UpdateBootstrapServiceTests
    {
        [Fact]
        public void IsUpdateBootstrapPath_PathUnderUpdatesFolder_ReturnsTrue()
        {
            var appData = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            var updatesDir = Path.Combine(appData, "updates");

            try
            {
                Directory.CreateDirectory(updatesDir);
                var exePath = Path.Combine(updatesDir, "CashTracker.exe");
                File.WriteAllText(exePath, "stub");

                Assert.True(UpdateBootstrapService.IsUpdateBootstrapPath(exePath, appData));
            }
            finally
            {
                if (Directory.Exists(appData))
                    Directory.Delete(appData, true);
            }
        }

        [Theory]
        [InlineData("1.0.10", "1.0.10")]
        [InlineData("v1.0.10-beta", "v1.0.10-beta")]
        [InlineData(" 1.0.10 / hotfix ", "1.0.10hotfix")]
        [InlineData(null, "")]
        public void CreateVersionFolderName_NormalizesInput(string? version, string expected)
        {
            var actual = UpdateBootstrapService.CreateVersionFolderName(version);

            if (string.IsNullOrWhiteSpace(expected))
            {
                Assert.Matches("^\\d{14}$", actual);
                return;
            }

            Assert.Equal(expected, actual);
        }
    }
}
