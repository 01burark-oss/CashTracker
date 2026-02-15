using System;

namespace CashTracker.Core.Entities
{
    public sealed class Isletme
    {
        public int Id { get; set; }
        public string Ad { get; set; } = string.Empty;
        public bool IsAktif { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
