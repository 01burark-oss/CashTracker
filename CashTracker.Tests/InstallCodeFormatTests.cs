using CashTracker.Core.Utilities;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class InstallCodeFormatTests
    {
        [Fact]
        public void TryNormalize_AcceptsValidInstallCode()
        {
            var ok = InstallCodeFormat.TryNormalize(" cti-1234abcd-5678ef90-1357ace0 ", out var normalized);

            Assert.True(ok);
            Assert.Equal("CTI-1234ABCD-5678EF90-1357ACE0", normalized);
        }

        [Fact]
        public void TryNormalize_RejectsMalformedInstallCode()
        {
            var ok = InstallCodeFormat.TryNormalize("CTI-1234-5678", out var normalized);

            Assert.False(ok);
            Assert.Equal(string.Empty, normalized);
        }
    }
}
