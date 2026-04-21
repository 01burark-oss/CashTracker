using System;

namespace CashTracker.Core.Entities
{
    public sealed class CariKart
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string Tip { get; set; } = "Musteri"; // Musteri | Tedarikci | HerIkisi
        public string Unvan { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;
        public string Eposta { get; set; } = string.Empty;
        public string Adres { get; set; } = string.Empty;
        public string VergiNoTc { get; set; } = string.Empty;
        public string VergiDairesi { get; set; } = string.Empty;
        public bool Aktif { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
