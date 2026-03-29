using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class ReceiptOcrResult
    {
        public string Merchant { get; set; } = string.Empty;
        public DateTime? ReceiptDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal? ReceiptTotal { get; set; }
        public List<ReceiptOcrLineItem> Items { get; set; } = [];
    }

    public sealed class ReceiptOcrLineItem
    {
        public string RawName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CandidateKalem { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public bool NeedsUserInput { get; set; }
    }
}
