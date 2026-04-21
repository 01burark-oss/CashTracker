using System;

namespace CashTracker.Core.Entities
{
    public sealed class GibPortalIslemLog
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public int? FaturaId { get; set; }
        public DateTime Tarih { get; set; } = DateTime.Now;
        public string Islem { get; set; } = string.Empty;
        public bool Basarili { get; set; }
        public string Mesaj { get; set; } = string.Empty;
    }
}
