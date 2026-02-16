using System.Threading.Tasks;

namespace CashTracker.Core.Services
{
    public interface IAppSecurityService
    {
        Task<string> GetPinAsync();
        Task SetPinAsync(string pin);
    }
}
