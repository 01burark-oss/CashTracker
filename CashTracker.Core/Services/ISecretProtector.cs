namespace CashTracker.Core.Services
{
    public interface ISecretProtector
    {
        string Protect(string secret);
        bool TryUnprotect(string protectedSecret, out string secret);
    }
}
