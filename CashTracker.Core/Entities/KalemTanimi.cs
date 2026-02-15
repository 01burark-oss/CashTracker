using System;

namespace CashTracker.Core.Entities
{
    public sealed class KalemTanimi
    {
        public int Id { get; set; }
        public int IsletmeId { get; set; }
        public string Tip { get; set; } = "Gider"; // Gelir | Gider
        public string Ad { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
