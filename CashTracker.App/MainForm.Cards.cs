using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CashTracker.App.UI;
using CashTracker.Core.Entities;
using CashTracker.Core.Models;

namespace CashTracker.App
{
    internal sealed partial class MainForm
    {
        private Panel CreateDailyOverviewPanel(Color backColor, Color borderColor)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = backColor,
                Padding = new Padding(20, 18, 20, 18),
                Margin = new Padding(0, 0, 0, 14)
            };
            panel.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, borderColor, ButtonBorderStyle.Solid);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var title = new Label
            {
                Text = "Gun Ozeti",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(14f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                Margin = new Padding(0, 0, 0, 10)
            };
            layout.Controls.Add(title, 0, 0);

            var totals = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 12)
            };
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            totals.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(totals, 0, 1);

            totals.Controls.Add(CreateDailyTotalBox("Toplam Gelir", Color.FromArgb(17, 121, 85), out _lblDailyOverviewIncome), 0, 0);
            totals.Controls.Add(CreateDailyTotalBox("Toplam Gider", Color.FromArgb(173, 59, 56), out _lblDailyOverviewExpense), 1, 0);
            totals.Controls.Add(CreateDailyTotalBox("Net Kar", Color.FromArgb(31, 59, 93), out _lblDailyOverviewNet), 2, 0);

            var subtitle = new Label
            {
                Text = "Odeme Yontemine Gore Dagilim",
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.8f, FontStyle.Bold),
                ForeColor = BrandTheme.MutedText,
                Margin = new Padding(0, 0, 0, 8)
            };
            layout.Controls.Add(subtitle, 0, 2);

            var methods = CreateDailyMethodsTable();
            layout.Controls.Add(methods, 0, 3);

            return panel;
        }

        private static Panel CreateDailyTotalBox(string title, Color accent, out Label valueLabel)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(12, 10, 12, 10),
                MinimumSize = new Size(180, 84)
            };
            panel.Paint += (_, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, Color.FromArgb(214, 223, 235), ButtonBorderStyle.Solid);
                using var pen = new Pen(accent, 2f);
                e.Graphics.DrawLine(pen, 10, 8, panel.Width - 10, 8);
            };

            var titleLabel = new Label
            {
                Text = title,
                AutoSize = true,
                Font = BrandTheme.CreateFont(9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(93, 106, 122),
                Margin = new Padding(0)
            };

            valueLabel = new Label
            {
                Text = "0.00",
                AutoSize = true,
                Font = BrandTheme.CreateHeadingFont(18f, FontStyle.Bold),
                ForeColor = accent,
                Margin = new Padding(0, 8, 0, 0)
            };

            panel.Controls.Add(titleLabel);
            panel.Controls.Add(valueLabel);
            valueLabel.Location = new Point(0, 26);
            return panel;
        }

        private TableLayoutPanel CreateDailyMethodsTable()
        {
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 4,
                BackColor = Color.White,
                Margin = new Padding(0)
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            AddMethodHeader(table, "Yontem", 0);
            AddMethodHeader(table, "Gelir", 1);
            AddMethodHeader(table, "Gider", 2);

            AddMethodRow(table, 1, "Nakit", out _lblDailyNakitIncome, out _lblDailyNakitExpense);
            AddMethodRow(table, 2, "Kredi Karti", out _lblDailyKrediKartiIncome, out _lblDailyKrediKartiExpense);
            AddMethodRow(table, 3, "Havale", out _lblDailyHavaleIncome, out _lblDailyHavaleExpense);

            table.Paint += (_, e) => ControlPaint.DrawBorder(e.Graphics, table.ClientRectangle, Color.FromArgb(214, 223, 235), ButtonBorderStyle.Solid);
            return table;
        }

        private static void AddMethodHeader(TableLayoutPanel table, string text, int column)
        {
            var header = new Label
            {
                Text = text,
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(83, 95, 112),
                Margin = new Padding(8, 8, 8, 6)
            };
            table.Controls.Add(header, column, 0);
        }

        private static void AddMethodRow(
            TableLayoutPanel table,
            int rowIndex,
            string methodName,
            out Label income,
            out Label expense)
        {
            var method = new Label
            {
                Text = methodName,
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.6f, FontStyle.Bold),
                ForeColor = BrandTheme.Heading,
                Margin = new Padding(8, 8, 8, 8)
            };
            table.Controls.Add(method, 0, rowIndex);

            income = new Label
            {
                Text = "0.00",
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 121, 85),
                Margin = new Padding(8, 8, 8, 8)
            };
            table.Controls.Add(income, 1, rowIndex);

            expense = new Label
            {
                Text = "0.00",
                AutoSize = true,
                Font = BrandTheme.CreateFont(9.8f, FontStyle.Bold),
                ForeColor = Color.FromArgb(173, 59, 56),
                Margin = new Padding(8, 8, 8, 8)
            };
            table.Controls.Add(expense, 2, rowIndex);
        }

        private void ApplyDailyOverview(PeriodSummary summary, IReadOnlyCollection<Kasa> records)
        {
            _lblDailyOverviewIncome.Text = $"{summary.IncomeTotal:n2}";
            _lblDailyOverviewExpense.Text = $"{summary.ExpenseTotal:n2}";
            _lblDailyOverviewNet.Text = $"{summary.Net:n2}";
            _lblDailyOverviewNet.ForeColor = summary.Net >= 0
                ? Color.FromArgb(31, 59, 93)
                : Color.FromArgb(173, 59, 56);

            var byMethod = records
                .GroupBy(x => NormalizeOdemeYontemi(x.OdemeYontemi), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        Income: g.Where(x => IsIncomeTip(x.Tip)).Sum(x => x.Tutar),
                        Expense: g.Where(x => IsExpenseTip(x.Tip)).Sum(x => x.Tutar)),
                    StringComparer.OrdinalIgnoreCase);

            ApplyDailyMethodValues("Nakit", byMethod, _lblDailyNakitIncome, _lblDailyNakitExpense);
            ApplyDailyMethodValues("KrediKarti", byMethod, _lblDailyKrediKartiIncome, _lblDailyKrediKartiExpense);
            ApplyDailyMethodValues("Havale", byMethod, _lblDailyHavaleIncome, _lblDailyHavaleExpense);
        }

        private static void ApplyDailyMethodValues(
            string method,
            IReadOnlyDictionary<string, (decimal Income, decimal Expense)> byMethod,
            Label income,
            Label expense)
        {
            if (!byMethod.TryGetValue(method, out var values))
            {
                income.Text = "0.00";
                expense.Text = "0.00";
                return;
            }

            income.Text = $"{values.Income:n2}";
            expense.Text = $"{values.Expense:n2}";
        }

        private static SummaryCard CreateSummaryCard(string title, Color backColor, Color accent, string buttonText, Color borderColor)
        {
            var panel = new Panel
            {
                Width = 330,
                Height = 236,
                MinimumSize = new Size(280, 220),
                BackColor = backColor,
                Margin = new Padding(0, 0, 16, 16),
                Padding = new Padding(18, 16, 18, 14)
            };

            panel.Paint += (_, e) =>
            {
                ControlPaint.DrawBorder(e.Graphics, panel.ClientRectangle, borderColor, ButtonBorderStyle.Solid);
                using var accentPen = new Pen(accent, 3f);
                e.Graphics.DrawLine(accentPen, 10, 9, panel.Width - 12, 9);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var titleLabel = new Label
            {
                Text = title,
                Font = BrandTheme.CreateHeadingFont(11.5f, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };
            layout.Controls.Add(titleLabel, 0, 0);

            var metrics = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 3,
                AutoSize = true,
                Margin = new Padding(0)
            };
            metrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            metrics.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(metrics, 0, 1);

            var income = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(17, 121, 85),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            var expense = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(173, 59, 56),
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            };
            var net = new Label
            {
                AutoSize = true,
                ForeColor = Color.FromArgb(35, 49, 74),
                Font = BrandTheme.CreateHeadingFont(11.6f, FontStyle.Bold),
                Margin = new Padding(0)
            };

            metrics.Controls.Add(income, 0, 0);
            metrics.Controls.Add(expense, 0, 1);
            metrics.Controls.Add(net, 0, 2);

            var sendButton = new Button
            {
                Text = buttonText,
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = BrandTheme.Navy,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = BrandTheme.CreateFont(10f, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 0)
            };
            sendButton.FlatAppearance.BorderColor = Color.FromArgb(21, 38, 61);
            sendButton.FlatAppearance.BorderSize = 1;
            sendButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 53, 92);
            layout.Controls.Add(sendButton, 0, 3);

            return new SummaryCard
            {
                Root = panel,
                Income = income,
                Expense = expense,
                Net = net,
                SendButton = sendButton
            };
        }

        private static void ApplySummary(SummaryCard card, PeriodSummary s)
        {
            card.Income.Text = $"Gelir: {s.IncomeTotal:n2}";
            card.Expense.Text = $"Gider: {s.ExpenseTotal:n2}";
            card.Net.Text = $"Net: {s.Net:n2}";
        }

        private static bool IsIncomeTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gelir" or "giris" or "income";
        }

        private static bool IsExpenseTip(string? tip)
        {
            var normalized = NormalizeAscii(tip);
            return normalized is "gider" or "cikis" or "expense";
        }

        private static string NormalizeOdemeYontemi(string? value)
        {
            var normalized = NormalizeAscii(value);
            return normalized switch
            {
                "nakit" => "Nakit",
                "cash" => "Nakit",
                "kredikarti" => "KrediKarti",
                "kredi karti" => "KrediKarti",
                "kart" => "KrediKarti",
                "creditcard" => "KrediKarti",
                "credit card" => "KrediKarti",
                "havale" => "Havale",
                "transfer" => "Havale",
                "bank transfer" => "Havale",
                _ => "Nakit"
            };
        }

        private static string NormalizeAscii(string? value)
        {
            return (value ?? string.Empty)
                .Trim()
                .ToLowerInvariant()
                .Replace('\u0131', 'i')
                .Replace('\u015f', 's')
                .Replace('\u011f', 'g')
                .Replace('\u00fc', 'u')
                .Replace('\u00f6', 'o')
                .Replace('\u00e7', 'c');
        }
    }
}

