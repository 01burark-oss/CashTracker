namespace CashTracker.Core.Entities
{
    public sealed class FaturaSatir
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int FaturaId { get; set; }
        public int? UrunHizmetId { get; set; }
        public string Aciklama { get; set; } = string.Empty;
        public string Birim { get; set; } = "Adet";
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal IskontoOrani { get; set; }
        public decimal IskontoTutar { get; set; }
        public decimal KdvOrani { get; set; } = 20m;
        public decimal KdvTutar { get; set; }
        public decimal SatirNetTutar { get; set; }
        public decimal SatirToplam { get; set; }
        public bool StokEtkilesin { get; set; } = true;
    }
}
