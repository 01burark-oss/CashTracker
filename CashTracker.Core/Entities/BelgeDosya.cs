using System;

namespace CashTracker.Core.Entities
{
    public sealed class BelgeDosya
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int FaturaId { get; set; }
        public string BelgeTipi { get; set; } = "PDF"; // PDF | XML | HTML
        public string DosyaYolu { get; set; } = string.Empty;
        public string Kaynak { get; set; } = "Yerel";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
