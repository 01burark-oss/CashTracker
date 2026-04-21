using System;

namespace CashTracker.Core.Entities
{
    public sealed class GibPortalAyar
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string KullaniciKodu { get; set; } = string.Empty;
        public string SifreCipherText { get; set; } = string.Empty;
        public bool TestModu { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
