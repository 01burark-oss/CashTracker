using System;

namespace CashTracker.Core.Models
{
    public sealed class PeriodSummary
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal IncomeTotal { get; set; }
        public decimal ExpenseTotal { get; set; }
        public int IncomeCount { get; set; }
        public int ExpenseCount { get; set; }
        public decimal Net => IncomeTotal - ExpenseTotal;
    }
}
