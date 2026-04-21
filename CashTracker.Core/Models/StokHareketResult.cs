using CashTracker.Core.Entities;

namespace CashTracker.Core.Models
{
    public sealed class StokHareketResult
    {
        public StokHareket Hareket { get; set; } = new StokHareket();
        public decimal MevcutStok { get; set; }
        public bool IsNegative => MevcutStok < 0;
    }
}
