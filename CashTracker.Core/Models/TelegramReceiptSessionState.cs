using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class TelegramReceiptSessionState
    {
        public long ChatId { get; set; }
        public long UserId { get; set; }
        public int SourceMessageId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string TempFilePath { get; set; } = string.Empty;
        public string Merchant { get; set; } = string.Empty;
        public DateTime? ReceiptDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal? ReceiptTotal { get; set; }
        public ReceiptSessionStep Step { get; set; } = ReceiptSessionStep.ResolveItems;
        public int CurrentItemIndex { get; set; } = -1;
        public string PendingCategoryName { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
        public List<TelegramReceiptSessionItem> Items { get; set; } = [];
    }

    public sealed class TelegramReceiptSessionItem
    {
        public string RawName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CandidateKalem { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public bool NeedsUserInput { get; set; }
        public string FinalKalem { get; set; } = string.Empty;
    }

    public enum ReceiptSessionStep
    {
        ResolveItems = 0,
        ConfirmNewCategory = 1,
        ResolveDate = 2,
        ResolvePaymentMethod = 3,
        AwaitFinalConfirmation = 4
    }
}
