using CashTracker.Infrastructure.Services;
using Xunit;

namespace CashTracker.Tests
{
    public sealed class SecretProtectorTests
    {
        [Fact]
        public void DpapiSecretProtector_EncryptsAndDecrypts_CurrentUserSecret()
        {
            var protector = new DpapiSecretProtector();
            const string secret = "gib-password-123";

            var cipher = protector.Protect(secret);

            Assert.NotEqual(secret, cipher);
            Assert.StartsWith("dpapi1:", cipher);
            Assert.True(protector.TryUnprotect(cipher, out var clear));
            Assert.Equal(secret, clear);
        }

        [Fact]
        public void DpapiSecretProtector_ReturnsFalse_ForBrokenCiphertext()
        {
            var protector = new DpapiSecretProtector();

            var ok = protector.TryUnprotect("dpapi1:not-base64", out var clear);

            Assert.False(ok);
            Assert.Equal(string.Empty, clear);
        }
    }
}
