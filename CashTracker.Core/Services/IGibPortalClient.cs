using System.Threading;
using System.Threading.Tasks;
using CashTracker.Core.Models;

namespace CashTracker.Core.Services
{
    public interface IGibPortalClient
    {
        Task<GibPortalResult> TestLoginAsync(string kullaniciKodu, string sifre, bool testModu, CancellationToken ct = default);
        Task<GibPortalResult> CreateDraftAsync(FaturaDetail fatura, string kullaniciKodu, string sifre, bool testModu, CancellationToken ct = default);
        Task<GibPortalResult> StartSmsVerificationAsync(string uuid, string kullaniciKodu, string sifre, bool testModu, CancellationToken ct = default);
        Task<GibPortalResult> CompleteSmsVerificationAsync(string uuid, string operationId, string smsCode, string kullaniciKodu, string sifre, bool testModu, CancellationToken ct = default);
    }
}
