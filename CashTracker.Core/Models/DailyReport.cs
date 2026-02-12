using System;

namespace CashTracker.Core.Models
{
    public sealed class DailyReport
    {
        public DateTime Date { get; set; }
        public decimal IncomeTotal { get; set; }
        public decimal ExpenseTotal { get; set; }
        public int IncomeCount { get; set; }
        public int ExpenseCount { get; set; }
    }
}
