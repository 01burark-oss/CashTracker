using System;

namespace CashTracker.Core.Models
{
    public sealed class TelegramStockSessionState
    {
        public long ChatId { get; set; }
        public long UserId { get; set; }
        public int SourceMessageId { get; set; }
        public StockSessionStep Step { get; set; } = StockSessionStep.AwaitBarcode;
        public string Barcode { get; set; } = string.Empty;
        public decimal PendingQuantity { get; set; }
        public int? ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Unit { get; set; } = "Adet";
        public decimal VatRate { get; set; } = 20m;
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CriticalStock { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public enum StockSessionStep
    {
        AwaitBarcode = 0,
        AwaitProductName = 1,
        AwaitUnit = 2,
        AwaitVatRate = 3,
        AwaitPurchasePrice = 4,
        AwaitSalePrice = 5,
        AwaitCriticalStock = 6,
        AwaitConfirmation = 7
    }
}
