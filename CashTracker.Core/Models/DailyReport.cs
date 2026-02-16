using System;
using System.Collections.Generic;

namespace CashTracker.Core.Models
{
    public sealed class DailyPaymentMethodBreakdown
    {
        public string Method { get; set; } = "Nakit";
        public decimal IncomeTotal { get; set; }
        public decimal ExpenseTotal { get; set; }
        public decimal Net => IncomeTotal - ExpenseTotal;
    }

    public sealed class DailyReport
    {
        public DateTime Date { get; set; }
        public decimal IncomeTotal { get; set; }
        public decimal ExpenseTotal { get; set; }
        public int IncomeCount { get; set; }
        public int ExpenseCount { get; set; }
        public List<DailyPaymentMethodBreakdown> PaymentMethodBreakdowns { get; set; } = new();
    }
}
